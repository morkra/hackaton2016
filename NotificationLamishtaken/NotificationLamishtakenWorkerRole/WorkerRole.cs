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
        const string SiteUrl = "https://www.dira.moch.gov.il/ProjectsList";
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

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
                if (DateTime.UtcNow.Minute % 5 == 0)
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
                Diagnostics.TrackTrace("DoWork started", Diagnostics.Severity.Information);
                var currentDirectory = Directory.GetCurrentDirectory();
                var fileName = Path.Combine(currentDirectory, "phantomjs.exe");

                Diagnostics.TrackTrace(string.Format("Loading driver from {0}", fileName), Diagnostics.Severity.Information);

                // TODO: do this only if file does not exist
                using (var w = new BinaryWriter(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                {
                    w.Write(Resources.phantomjs);
                }

                m_phantomJs = new PhantomJSDriver(currentDirectory);

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
                foreach (var row in rows)
                {
                    ICollection<IWebElement> columns = row.FindElements(By.TagName("td"));
                    IEnumerable<string> texts = columns.Select(col => col.Text);
                    Diagnostics.TrackTrace(string.Join(", ", texts), Diagnostics.Severity.Information);
                }
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
    }
}
