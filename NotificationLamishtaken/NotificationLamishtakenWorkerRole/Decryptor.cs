using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace NotificationLamishtakenWorkerRole
{
    public static class Decryptor
    {
        public static string Decrypt(string encryptedItem)
        {
            if (string.IsNullOrWhiteSpace(encryptedItem))
            {
                return encryptedItem;
            }

            var envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(Convert.FromBase64String(encryptedItem));
            envelopedCms.Decrypt();
            return Encoding.UTF8.GetString(envelopedCms.ContentInfo.Content);
        }
    }
}
