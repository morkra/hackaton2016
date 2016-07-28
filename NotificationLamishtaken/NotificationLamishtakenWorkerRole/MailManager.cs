using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace NotificationLamishtakenWorkerRole
{
    public static class MailManager
    {
        private const string smtpHostName = "smtp.gmail.com";
        private const string emailUserName = "mechirlamishtaken";
        private const string sourceEmail = "mechirlamishtaken@mail.com";


        public static void Publish(string mailBody)
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
                    Credentials = new NetworkCredential(emailUserName, passwordDecrypted),
                    EnableSsl = true
                };

                mail.Bcc.Add(emailToList);
                mail.From = new MailAddress(sourceEmail);
                mail.Subject = "מחיר למשתכן - פרוייקטים חדשים נפתחו להרשמה";
                mail.IsBodyHtml = true;
                mail.Body = mailBody;
                client.Send(mail);
            }
            catch (SmtpException ex)
            {
                Diagnostics.TrackException(ex, 1, ex.Message);
                throw;
            }
        }

        public static string BuildNewProjectsMailBody(List<ProjectProperties> projects)
        {
            string emailTitle = "להלן רשימת הפרוייקטים הפתוחים החל מהיום להרשמה";
            string emailText = String.Join("<br>", projects.Select(p => p.ToStringHTML()).ToArray());

            return "<!DOCTYPE html> " +
               "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
               "<body style=\"font-family:'Century Gothic'\">" +
                   "<h3 style=\"text-align:right;\">" + emailTitle + "</h3>" +
                   "<p style=\"text-align:right;\">" + emailText + "</p>" +
               "</body>" +
               "</html>";
        }
    }
}
