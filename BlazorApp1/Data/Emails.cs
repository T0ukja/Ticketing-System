using Microsoft.Exchange.WebServices.Data;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BlazorApp1.Data
{

    public class Emails
    {
      
        public List<EmailMessage>? Getemails()
        {
            string[] credentials = File.ReadAllText(@".\credentials.txt").Split(new[] { ' ' }); ;
            string emailaddress = credentials[0];
            string password = credentials[1];
            string emailurl = credentials[2];
          


            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2007_SP1);
            service.Credentials = new WebCredentials(emailaddress,password);
            service.TraceEnabled = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            
            // service.TraceFlags = TraceFlags.All;
            // service.UseDefaultCredentials = true;
            FindItemsResults<Item> findResults;
            int offset = 0;
            int pageSize = 20;
            ItemView view = new ItemView(pageSize, offset, OffsetBasePoint.Beginning);
            service.Url = new Uri(emailurl);
            findResults = service.FindItems(WellKnownFolderName.Inbox, view);
            List<EmailMessage> Unreaded = new List<EmailMessage>();



            foreach (EmailMessage message in findResults.Items)
            {
                if (message.IsRead == false) //if the current message is unread
                {       
                    Unreaded.Add(message);  
                }
                else
                {
                }
             
            }
            return Unreaded;
     
        }
        static bool RedirectionCallback(string url)
        {
            // Return true if the URL is an HTTPS URL.
            return url.ToLower().StartsWith("https://");
        }
        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;
            Uri redirectionUri = new Uri(redirectionUrl);
            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
