using Microsoft.Exchange.WebServices.Data;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BlazorApp1.Data
{

    public class Emails
    {

          public List<ConversationNode>? getconversion(string messageid)
        //  public Collection<Item>? getconversion(string messageid)
        {
         
            string[] credentials = File.ReadAllText(@".\credentials.txt").Split(new[] { ' ' }); ;
            string emailaddress = credentials[0];
            string password = credentials[1];
            string emailurl = credentials[2];
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials = new WebCredentials(emailaddress, password);
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.Url = new Uri(emailurl);
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            //Collection<Item> items = new Collection<Item>();
            List<ConversationNode> items = new List<ConversationNode>();
            try
            {
                // Find an item in a conversation. Find the first item.
                // FindItemsResults<Item> results = service.FindItems(WellKnownFolderName.Inbox, UserId.Equals(messageid), new ItemView(1));
                /*  FindItems(WellKnownFolderName.Inbox,
                                                                 new ItemView());*/
                EmailMessage email = EmailMessage.Bind(service, new ItemId(messageid));
                // Get the conversation identifier of an item. 
                ConversationId convId = email.ConversationId;
                //      Console.WriteLine(convId, "Convid tässä");
                // Specify the properties that will be 
                // returned for the items in the conversation.
                PropertySet properties = new PropertySet(BasePropertySet.IdOnly,
                                                          ItemSchema.Subject,
                                                          ItemSchema.DateTimeReceived, ItemSchema.TextBody, ItemSchema.IsFromMe);

                // Identify the folders to ignore.
                Collection<FolderId> foldersToIgnore = new Collection<FolderId>() { WellKnownFolderName.DeletedItems, WellKnownFolderName.Drafts };

                // Request conversation items. This results in a call to the service.         
                ConversationResponse response = service.GetConversationItems(convId,
                                                                               properties,
                                                                               null,
                                                                               foldersToIgnore,
                                                                               ConversationSortOrder.TreeOrderDescending);

                // Get the synchronization state of the conversation.
 

                foreach (ConversationNode node in response.ConversationNodes)
                {
                   // Console.WriteLine("Parent conversation index: " + node.Items[0].Subject);
                    items.Add(node);
                    /*foreach (Item item in node.Items)
                    {
                        Console.WriteLine("   Item ID: " + item.Id.UniqueId);
                        Console.WriteLine("   Subject: " + item.Subject);
                        Console.WriteLine("   Sender: " + item.IsFromMe);
                        Console.WriteLine("   Received: " + item.DateTimeReceived);
                        Console.WriteLine("   Body: " + item.TextBody.Text);
                        items.Add(item);
                    }*/
                }
            }
            // This exception may occur if there is an error with the service.
            catch (ServiceResponseException srException)
            {
                Console.WriteLine(srException);
            }
            return items;
        }

//*****************************************************************************************
        public List<EmailMessage>? Getemails()
        {
            string[] credentials = File.ReadAllText(@".\credentials.txt").Split(new[] { ' ' }); ;
            string emailaddress = credentials[0];
            string password = credentials[1];
            string emailurl = credentials[2];
          


            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
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
