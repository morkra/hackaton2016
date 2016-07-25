using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Hackaton2016
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver ChromeInstance = new ChromeDriver("C:\\");
            ChromeInstance.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(60));
            /*
            ChromeInstance.Navigate().GoToUrl("http://www.w3schools.com/html/html_tables.asp");
            WebElement_Table = ChromeInstance.FindElement(By.XPath("//h3[contains(.,'HTML Table Example')]"));*/
            ChromeInstance.Navigate().GoToUrl("https://www.dira.moch.gov.il/ProjectsList");
            var selectionLayout = ChromeInstance.FindElement(By.CssSelector("div.row.col-lg-12.col-md-12.col-xs-12.dark-blue-box"));
            var projectStateSelectionLayout = selectionLayout.FindElement(By.CssSelector("div.col-lg-3.col-md-3.col-xs-12"));

            var button = selectionLayout.FindElement(By.Id("s2id_slctStatus"));
            button.Click();

        }
    }
}
