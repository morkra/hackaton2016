using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using NotificationLamishtakenWorkerRole.Properties;

namespace NotificationLamishtakenWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private const string SiteUrl = "https://www.dira.moch.gov.il/ProjectsList";
        private const string DriverName = "phantomjs.exe";
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static string m_currentDirectory = Directory.GetCurrentDirectory();
        private const string smtpHostName = "smtp.gmail.com";
        private const string emailUserName = "mechirlamishtaken";
        private const string sourceEmail = "mechirlamishtaken@mail.com";
        public override void Run()
        {
            Trace.TraceInformation("NotificationLamishtakenWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("NotificationLamishtakenWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("NotificationLamishtakenWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("NotificationLamishtakenWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                // Scheduled to run every 5 minutes
                //if (DateTime.UtcNow.Minute % 5 == 0)
                {
                    DoWork();
                }
                await Task.Delay(1000);
            }
        }

        private static void DoWork()
        {
            PhantomJSDriver m_phantomJs = null;
            try
            {
                Diagnostics.TrackTrace("DoWork() started", Diagnostics.Severity.Information);

                var driverFilePath = Path.Combine(m_currentDirectory, DriverName);
                InstallDriverIfNotExist(driverFilePath);

                Diagnostics.TrackTrace(string.Format("Loading driver from {0}", driverFilePath), Diagnostics.Severity.Information);

                m_phantomJs = new PhantomJSDriver(m_currentDirectory);
                m_phantomJs.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(60));

                // Open URL
                Diagnostics.TrackTrace(string.Format("Go to target URL: {0}", SiteUrl), Diagnostics.Severity.Information);
                m_phantomJs.Navigate().GoToUrl(SiteUrl);

                // Get relevant element 
                var selectionLayout = m_phantomJs.FindElement(By.CssSelector("div.row.col-lg-12.col-md-12.col-xs-12.dark-blue-box"));
                var selector = selectionLayout.FindElement(By.Id("slctStatus"));
                var selectElement = new SelectElement(selector);

                // Select relevant topic
                selectElement.SelectByText("פתוח להרשמה לציבור");

                // Go
                var wait = new WebDriverWait(m_phantomJs, TimeSpan.FromSeconds(10));
                var button = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a.btn.btn-success.btn-green")));

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int) TimeSpan.FromSeconds(5).TotalMilliseconds);

                button.Click();

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int) TimeSpan.FromSeconds(5).TotalMilliseconds);

                // Get the table with relevant data
                var table = m_phantomJs.FindElement(By.ClassName("table-responsive"));

                // Get table rows
                IWebElement tableBody = table.FindElement(By.TagName("tbody"));
                ICollection<IWebElement> rows = tableBody.FindElements(By.TagName("tr"));

                List<ProjectProperties> OpenRegistrationProjects = new List<ProjectProperties>();
                foreach (var row in rows)
                {
                    var columns = row
                        .FindElements(By.TagName("td"))
                        .Select(col => col.Text)
                        .ToArray();

                    OpenRegistrationProjects.Add(new ProjectProperties(columns));
                }

                // Add project for tests only
                OpenRegistrationProjects.Add(new ProjectProperties(RegistrationStatus.Open, "999", DateTime.Now, DateTime.Now.AddDays(10), "כפר שמריהו", "שמריהו הירוקה"));

                // Get new projects opened for registration today
                List<ProjectProperties> newProjects = GetNewProjectsOpenForRegistration(OpenRegistrationProjects);
                Diagnostics.TrackTrace(string.Format("Found {0} projects where registration start on {1}", newProjects.Count, DateTime.Today), Diagnostics.Severity.Information);

                List<ProjectProperties> nearExpiredProjects = GetNearExpiredProjectsOpenForRegistration(OpenRegistrationProjects);
                Diagnostics.TrackTrace(string.Format("Found {0} projects where registration to be expired tomorrow", newProjects.Count), Diagnostics.Severity.Information);

                // Send email
                //Publish(newProjects);
                Diagnostics.TrackTrace("DoWork() completed successfully", Diagnostics.Severity.Information);
            }
            catch (Exception ex)
            {
                Diagnostics.TrackException(ex, 1, ex.Message);
                throw;
            }
            finally
            {
                m_phantomJs?.Dispose();
            }
        }

        private static void InstallDriverIfNotExist(string driverFilePath)
        {
            if (File.Exists(driverFilePath))
            {
                return;
            }

            // Create the driver file
            using (var w = new BinaryWriter(new FileStream(driverFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                )
            {
                w.Write(Resources.phantomjs);
            }
        }

        private static List<ProjectProperties> GetNewProjectsOpenForRegistration(List<ProjectProperties> projects)
        {
            return projects
                .Where(p => p.StartDate.Date == DateTime.Today)
                .ToList();
        }

        private static List<ProjectProperties> GetNearExpiredProjectsOpenForRegistration(List<ProjectProperties> projects)
        {
            return projects
                .Where(p => p.EndDate.Date == DateTime.Today.AddDays(1))
                .ToList();
        }

        private static void Publish(List<ProjectProperties> Projects)
        {
            var passwordDecrypted = Decryptor.Decrypt(ConfigurationManager.AppSettings["emailPassword"]);
            var emailToList = Decryptor.Decrypt(ConfigurationManager.AppSettings["emailToList"]);

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient client = new SmtpClient
                {
                    Port = 25,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = smtpHostName,
                    Credentials = new System.Net.NetworkCredential(emailUserName, passwordDecrypted),
                    EnableSsl = true
                };
                mail.Bcc.Add(emailToList);
                mail.From = new MailAddress(sourceEmail);
                mail.Subject = "מחיר למשתכן - פרוייקטים חדשים נפתחו להרשמה";
                mail.Body = BuildBody(Projects);
                client.Send(mail);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static string BuildBody(List<ProjectProperties> projects)
        {
            string emailBody = "להלן רשימת הפרוייקטים הפתוחים החל מהיום להרשמה:   ";
            return emailBody + Environment.NewLine + string.Join(Environment.NewLine,projects.Select(p => p.ToString()).ToArray());
        }
    }
}
