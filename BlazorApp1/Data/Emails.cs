using Microsoft.Exchange.WebServices.Data;
using Microsoft.JSInterop;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly IMongoCollection<Datamodel> emailCollection;

        private MongoClient mongoClient { get; set; }
        private readonly IMongoDatabase mongoDatabase;


        // Variables for setting credentials from txt file.
        private string[] credentials;
        private string emailAddress;
        private string password;
        private string emailurl;
        public IMemoryCache MemoryCache { get; }

        // Needed for Microsoft EWS.
        private ExchangeService service;


        public Emails(IOptions<Settingsmodel> settingsmodel, IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
            // Settings model is used as class to read Mongodb
            // Settings Mongodb values from appsettings.json
            mongoClient = new MongoClient(
            settingsmodel.Value.ConnectionString);

            mongoDatabase = mongoClient.GetDatabase(
                settingsmodel.Value.DatabaseName);

            emailCollection = mongoDatabase.GetCollection<Datamodel>(
                settingsmodel.Value.CollectionName);

            // Emailmessage for sending messages

            // Reading password/username credentials from Cred.txt file. This file is hidden from github.
            credentials = File.ReadAllText(@".\Cred.txt").Split(new[] { ' ' }); ;
            emailAddress = credentials[0];
            password = credentials[1];
            emailurl = credentials[2];

            // Settings ExchangeService.
            service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials = new WebCredentials(emailAddress, password);
            service.TraceEnabled = true;
            service.TraceFlags = TraceFlags.All;
            service.Url = new Uri(emailurl);
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }



        public async Task<ConversationResponse> GetConversion(string messageId)
        {
            // List declaration for items in conversation.
            var sw = new Stopwatch();
            sw.Start();
            ConversationResponse response;


       List<ConversationNode> items = new List<ConversationNode>();
           // Try catch block.
           try
           {



               // Object for reading email.
               EmailMessage email = EmailMessage.Bind(service, new ItemId(messageId));
       // Get the conversation identifier of an item. 
       ConversationId convId = email.ConversationId;
       // Properties for what to return for each email.
       PropertySet properties = new PropertySet(BasePropertySet.IdOnly,
                                                 ItemSchema.Subject,
                                                 ItemSchema.DateTimeReceived, ItemSchema.TextBody, ItemSchema.IsFromMe);

       // Identify the folders to ignore.
       Collection<FolderId> foldersToIgnore = new Collection<FolderId>() { WellKnownFolderName.DeletedItems, WellKnownFolderName.Drafts };

       // Request conversation items. This results in a call to the service.         
       response = service.GetConversationItems(convId, properties, null, foldersToIgnore, ConversationSortOrder.TreeOrderDescending);

      /* await System.Threading.Tasks.Task.Run(() =>
               {
           // Get the synchronization state of the conversation.
           Parallel.ForEach(response.ConversationNodes, node =>
           {
               items.Add(node);

           });
       });

                */
           }
           // This exception may occur if there is an error with the service.
           catch (ServiceResponseException srException)
           {
               Console.WriteLine(srException);
                response = null;
           }

    // Returns conversation items which is later used in site.
    return response;

   
        }






        // Function used to get messages from database
        public async Task<List<Datamodel>> GetMessagesDbAsync()
        {
           
             List<Datamodel> listOfEmails = emailCollection.Find(x => x.handler.Equals("")).ToList();
            return listOfEmails;

        }



    // Function used to get unreaded emails and insert them to database.
    public void GetEmails()
        {
            EmailMessage helpdeskemail = new EmailMessage(service);


            // Variable declaration.
            FindItemsResults<Item> findResults;
            // offSet
            int offSet = 0;
            // How many to get at function call (loads this many messages at one time).
            int pageSize = 20;
            // Creates view and uses variables above
            ItemView view = new ItemView(pageSize, offSet, OffsetBasePoint.Beginning);
            
            // Assings email service url.
            service.Url = new Uri(emailurl);
            // finds results from email folder (this is set to Inbox).
            findResults = service.FindItems(WellKnownFolderName.Inbox, view);
            // Lists emails to list.
            //   List<Datamodel> emailModel = new List<Datamodel>();
            Datamodel emailModel = new Datamodel();
            // Session variable for mongoclient.
            var session = mongoClient.StartSession();
            // Bool value to prevent error if there arent't emails then no need to update.
           
            // Foreach loop to read messages.
            foreach (EmailMessage message in findResults.Items)
            {
                // If message is unreaded
                if ((message.IsRead == false) && (message.InReplyTo == null)) //if the current message is unread
                {
                  // Adds data to emailModel and then push's it to database.
                   emailModel.subject = message.Subject;
                   emailModel.sender = message.Sender.Address.ToString();
                   emailModel.attachment = message.Attachments.Count().ToString();
                   emailModel.message_id = message.Id.ToString();
                   emailModel.datetimecreated = message.DateTimeCreated.ToString();
                   emailModel.datetimereceived = message.DateTimeReceived.ToString();
                   emailModel.handler = "";
                   emailModel.status = "New";
                    // Sets email readed.
                    message.IsRead = true;
          
                    //Updates email state
                    message.Update(ConflictResolutionMode.AutoResolve);
                    try {
                        //Inserts email to database collection
                        emailCollection.InsertOneAsync(emailModel);
                        // Reply message string





                        // While loop to check when message is readed, then sends reply mail.
                     while (message.IsRead == false)
                        {
                            message.Load();
                     
                        }
                     if(message.IsRead == true)
						{
                            string replyMessage = "Tämä on järjestelmäpalvelun automaattinen viesti. Olemme vastaanottaneet tukipyyntösi ja se tullaan käsittelemään mahdollisimman pian." +
    "Jos haluat lähettää lisätietoja tukipyyntöösi liittyen, vastaa tähän sähköpostiin.\n" +
    "Tksystem palvelu";

                            message.Load(new PropertySet(ItemSchema.MimeContent));
                            bool replyToAll = false;
                            ResponseMessage responseMessage = message.CreateReply(replyToAll);
                            responseMessage.BodyPrefix = replyMessage;
                      
                            responseMessage.SendAndSaveCopy();
                            // This true
                            //  bool replyToAll = true;
                            //  message.Reply(replyMessage, replyToAll);
                        }
                        

                        // The actual sending command with parameters.
                       
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine(e);
                    }

            
                
                }
                else
                {
                    // If email is readed but it's useless at moment.
                }
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
