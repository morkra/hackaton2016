using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
                Diagnostics.TrackTrace(string.Format("Loading driver from {0}", driverFilePath),
                    Diagnostics.Severity.Information);

                m_phantomJs = new PhantomJSDriver(m_currentDirectory);
                m_phantomJs.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(60));

                // Open URL
                Diagnostics.TrackTrace(string.Format("Go to target URL: {0}", SiteUrl), Diagnostics.Severity.Information);
                m_phantomJs.Navigate().GoToUrl(SiteUrl);

                // Get relevant element 
                var selectionLayout =
                    m_phantomJs.FindElement(By.CssSelector("div.row.col-lg-12.col-md-12.col-xs-12.dark-blue-box"));

                // var projectStateSelectionLayout = selectionLayout.FindElement(By.CssSelector("div.col-lg-3.col-md-3.col-xs-12"));
                var selector = selectionLayout.FindElement(By.Id("slctStatus"));
                var selectElement = new SelectElement(selector);

                // Select relevant topic
                selectElement.SelectByText("פתוח להרשמה לציבור");

                // Go
                var wait = new WebDriverWait(m_phantomJs, TimeSpan.FromSeconds(10));
                var button =
                    wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a.btn.btn-success.btn-green")));

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int) TimeSpan.FromSeconds(5).TotalMilliseconds);

                button.Click();

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int) TimeSpan.FromSeconds(5).TotalMilliseconds);

                // Now we have the table with relevant data
                var table = m_phantomJs.FindElement(By.ClassName("table-responsive"));

                // Gets table rows
                IWebElement tableBody = table.FindElement(By.TagName("tbody"));
                ICollection<IWebElement> rows = tableBody.FindElements(By.TagName("tr"));

                List<ProjectProperties> OpenRegistrationProjects = new List<ProjectProperties>();
                foreach (var row in rows)
                {
                    ICollection<IWebElement> columns = row.FindElements(By.TagName("td"));
                    var texts = columns.Select(col => col.Text).ToArray();
                    OpenRegistrationProjects.Add(new ProjectProperties(texts));
                }

                // Add project for test only
                OpenRegistrationProjects.Add(new ProjectProperties(RegistrationStatus.Open, "999", DateTime.Now, DateTime.Now.AddDays(10), "כפר שמריהו", "שמריהו הירוקה"));
                List<ProjectProperties> newProjects = GetNewProjectOpenForRegistration(OpenRegistrationProjects);
                Diagnostics.TrackTrace(string.Format("Found {0} projects where registration start on {1}", newProjects.Count, DateTime.Today), Diagnostics.Severity.Information);

                // TODO: Send email
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

        private static List<ProjectProperties> GetNewProjectOpenForRegistration(List<ProjectProperties> projects)
        {
            return projects
                .Where(p => p.StartDate.Date == DateTime.Today)
                .ToList();
        }

    }
}