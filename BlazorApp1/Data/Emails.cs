using Microsoft.Exchange.WebServices.Data;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp1.Data
{

    public class Emails
    {
        /*
        *******************************************************************************
        Declare own MongoDB settings in appsettings.json
        It's hided from Github for security reasons
        *******************************************************************************
         
         * */
        // Variable declares for MongoDB database.
        private readonly IMongoCollection<Datamodel> emailcollection;
        private MongoClient mongoClient { get; set; }
        private readonly IMongoDatabase mongoDatabase;


        // Variables for setting credentials from txt file.
        private string[] credentials;
        private string emailaddress;
        private string password;
        private string emailurl;


        // Needed for Microsoft EWS.
        private ExchangeService service;

        public Emails(IOptions<Settingsmodel> settingsmodel)
        {
            // Settings model is used as class to read Mongodb
            // Settings Mongodb values from appsettings.json
            mongoClient = new MongoClient(
            settingsmodel.Value.ConnectionString);

            mongoDatabase = mongoClient.GetDatabase(
                settingsmodel.Value.DatabaseName);

            emailcollection = mongoDatabase.GetCollection<Datamodel>(
                settingsmodel.Value.EmailCollectionName);



            // Reading password/username credentials from Cred.txt file. This file is hidden from github.
            credentials = File.ReadAllText(@".\Cred.txt").Split(new[] { ' ' }); ;
            emailaddress = credentials[0];
            password = credentials[1];
            emailurl = credentials[2];

            // Settings ExchangeService.
            service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials = new WebCredentials(emailaddress, password);
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.Url = new Uri(emailurl);
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }



        public List<ConversationNode>? getconversion(string messageid)
        {   // List declaration for items in conversation.
            List<ConversationNode> items = new List<ConversationNode>();
            // Try catch block.
            try
            {
                // Object for reading email.
                EmailMessage email = EmailMessage.Bind(service, new ItemId(messageid));
                // Get the conversation identifier of an item. 
                ConversationId convId = email.ConversationId;
                // Properties for what to return for each email.
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
                    items.Add(node);

                }
            }
            // This exception may occur if there is an error with the service.
            catch (ServiceResponseException srException)
            {
                Console.WriteLine(srException);
            }

            // Returns conversation items which is later used in site.
            return items;
        }




        // Function used to get messages from database
        public List<Datamodel> GetmessagesdbAsync()
        {
            List<Datamodel> ListOfEmails = emailcollection.Find(x => x.handler.Equals("")).ToList();
            return ListOfEmails;
        }
        
        // Function used to get unreaded emails and insert them to database.
        public void Getemails()
        {
            // Variable declaration.
            FindItemsResults<Item> findResults;
            // offset
            int offset = 0;
            // How many to get at function call (loads this many messages at one time).
            int pageSize = 20;
            // Creates view and uses variables above
            ItemView view = new ItemView(pageSize, offset, OffsetBasePoint.Beginning);
            
            // Assings email service url.
            service.Url = new Uri(emailurl);
            // finds results from email folder (this is set to Inbox).
            findResults = service.FindItems(WellKnownFolderName.Inbox, view);
            // Lists emails to list.
            List<Datamodel> Useremailmodel = new List<Datamodel>();
            // Session variable for mongoclient.
            var session = mongoClient.StartSession();
            // Bool value to prevent error if there arent't emails then no need to update.
           
            // Foreach loop to read messages.
            foreach (EmailMessage message in findResults.Items)
            {
                // If message is unreaded
                if (message.IsRead == false) //if the current message is unread
                {
                    // Add Datamodel to list (model object for database)
                    Useremailmodel.Add(new Datamodel { subject = message.Subject.ToString(), sender = message.Sender.ToString(), attachment = message.Attachments.Count().ToString(), message_id = message.Id.ToString(), datetimecreated = message.DateTimeCreated.ToString(), datetimereceived = message.DateTimeReceived.ToString(), handler = "", status = "New" });
                    // Sets email readed.
                    message.IsRead = true;
                    // Updates email "state".
                    message.Update(ConflictResolutionMode.AutoResolve);
                }
                else
                {
                    // If email is readed but it's useless at moment.
                }

            }
            bool isEmail = Useremailmodel.Any();

            // If list has new unreaded emails, then insert them to mongodb database.
            if (isEmail == true)
            {
                emailcollection.InsertMany(session, Useremailmodel);


            }
        }


        // Uselessa at the moment.
        static bool RedirectionCallback(string url)
        {
            // Return true if the URL is an HTTPS URL.
            return url.ToLower().StartsWith("https://");
        }

        // Useless at the moment.
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
