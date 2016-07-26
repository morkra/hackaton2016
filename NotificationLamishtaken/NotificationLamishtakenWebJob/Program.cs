using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace NotificationLamishtakenWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var host = new JobHost();
            IWebDriver browser;

            //Set Capabilities
            DesiredCapabilities capability = DesiredCapabilities.Chrome();

            //Run
            browser = new RemoteWebDriver(
                new Uri("http://hub.browserstack.com/wd/hub/"), capability
            );
            browser.Navigate().GoToUrl("http://hub.browserstack.com/wd/hub/");

            //Quit driver and dispose job
            browser.Quit();
            host.Dispose();
        }
    }
}
