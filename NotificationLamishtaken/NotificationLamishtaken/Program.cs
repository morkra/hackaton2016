using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace NotificationLamishtaken
{
    class Program
    {
        private static IWebDriver m_chromeInstance;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting...");

                // Preparations
                try
                {
                    Console.WriteLine("Loading chrome driver...");
                    m_chromeInstance = new ChromeDriver("C:\\");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Chrome driver does not exist! Exception: {0}", ex);
                    throw;
                }
                
                m_chromeInstance.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(60));

                // Open URL
                Console.WriteLine("Go to target URL: <URL>");
                m_chromeInstance.Navigate().GoToUrl("https://www.dira.moch.gov.il/ProjectsList");

                // Get relevant element 
                var selectionLayout = m_chromeInstance.FindElement(By.CssSelector("div.row.col-lg-12.col-md-12.col-xs-12.dark-blue-box"));

                // var projectStateSelectionLayout = selectionLayout.FindElement(By.CssSelector("div.col-lg-3.col-md-3.col-xs-12"));
                var selector = selectionLayout.FindElement(By.Id("slctStatus"));
                var selectElement = new SelectElement(selector);

                // Select relevant topic
                selectElement.SelectByText("פתוח להרשמה לציבור");

                // Go
                var wait = new WebDriverWait(m_chromeInstance, TimeSpan.FromSeconds(10));
                var button = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("a.btn.btn-success.btn-green")));

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int)TimeSpan.FromSeconds(5).TotalMilliseconds);

                button.Click();

                // TODO: find a better way to wait for the button to be clickable
                Thread.Sleep((int)TimeSpan.FromSeconds(5).TotalMilliseconds);

                // Now we have the table with relevant data
                var table = m_chromeInstance.FindElement(By.ClassName("table-responsive"));

                // Gets table rows
                IWebElement tableBody = table.FindElement(By.TagName("tbody"));
                ICollection<IWebElement> rows = tableBody.FindElements(By.TagName("tr"));
                foreach (var row in rows)
                {
                    ICollection<IWebElement> columns = row.FindElements(By.TagName("td"));
                    Console.WriteLine(string.Join(", ", columns));
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                m_chromeInstance.Quit();
            }
        }
    }
}
