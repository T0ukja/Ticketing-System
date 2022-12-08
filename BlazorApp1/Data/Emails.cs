using System.Collections.ObjectModel;
using System.Diagnostics;
using BlazorApp1.Models;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


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
        private readonly IMongoCollection<Comments> commentsCollection;
        private readonly IMongoCollection<NewEmails> newEmailsCollection;
        private readonly IMongoCollection<Datamodel> historyCollection;

        private MongoClient mongoClient { get; set; }
        private readonly IMongoDatabase mongoDatabase;
        private readonly IMongoDatabase mongoDatabase_comments;
        private readonly IMongoDatabase mongoDatabase_newemails;
        private readonly IMongoDatabase mongoDatabase_solved;
        private readonly ILogger _logger;

        // Variables for setting credentials from txt file.
        private string[] credentials;
        private string emailAddress;
        private string password;
        private string emailurl;


        //	PeriodicTimer periodicTimer = new(TimeSpan.FromHours(2));

        public IMemoryCache MemoryCache { get; }

        // Needed for Microsoft EWS.
        private ExchangeService service;


        public Emails(IOptions<Settingsmodel> settingsmodel, IOptions<Settingsmodel_comments> settingsmodel_comments, IOptions<Settingsmodel_newemails> settingsmodel_newemails, IOptions<Settingsmodel_solved> settingsmodel_solved, IMemoryCache memoryCache, ILogger<Login> logger)
        {
            _logger = logger;
            MemoryCache = memoryCache;
            // Settings model is used as class to read Mongodb
            // Settings Mongodb values from appsettings.json
            mongoClient = new MongoClient(
            settingsmodel.Value.ConnectionString);

            mongoDatabase_newemails = mongoClient.GetDatabase(
                settingsmodel_newemails.Value.DatabaseName);

            mongoDatabase_comments = mongoClient.GetDatabase(
                settingsmodel_comments.Value.DatabaseName);

            mongoDatabase_solved = mongoClient.GetDatabase(
                    settingsmodel_solved.Value.DatabaseName);

            mongoDatabase = mongoClient.GetDatabase(
                settingsmodel.Value.DatabaseName);

            // Reading ends.
            //*****************************************************
            // Assinging collections with 
            emailCollection = mongoDatabase.GetCollection<Datamodel>(
                settingsmodel.Value.CollectionName);

            commentsCollection = mongoDatabase.GetCollection<Comments>(
                settingsmodel_comments.Value.CollectionName);

            newEmailsCollection = mongoDatabase.GetCollection<NewEmails>(
            settingsmodel_newemails.Value.CollectionName);

            historyCollection = mongoDatabase.GetCollection<Datamodel>(
                settingsmodel_solved.Value.CollectionName);

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


        // For changing color code in list
        public async void UpdateColorStatus()
        {
            List<Datamodel> listofemails = new List<Datamodel>();
            listofemails = emailCollection.Find(_ => true).ToList();


            foreach (Datamodel moro in listofemails)
            {
                // Create datetime variable from Datamodel object.
                DateTime ticketdate = moro.datetimereceived;

                // Comparing datatimes to get hours.
                var result = (DateTime.UtcNow - moro.datetimereceived).TotalHours;

                // Options/filter to update values.
                var options = new UpdateOptions { IsUpsert = true };
                var filter = Builders<Datamodel>.Filter.Eq("_id", moro.id);


                // if else to set color code.
                if (result > 24 && result < 72)
                {
                    // yellow color
                    var update = Builders<Datamodel>.Update.Set("colorCode", "yellow");

                    emailCollection.UpdateOne(filter, update, options);


                }

                if (result > 72)
                {
                    // red color
                    var update = Builders<Datamodel>.Update.Set("colorCode", "red");

                    emailCollection.UpdateOne(filter, update, options);

                }
                else
                {

                    // stays green
                }
            }


        }


        // After reading unreaded mails, this is called to delete documents from collection by id.
        public async void DeleteUnreadedMails(string messageconversationid)
        {

            // If there is multiple unreaded mails in collection
            if ((newEmailsCollection.CountDocuments(x => x.EmailConversationId == messageconversationid)) > 1)
            {
                await newEmailsCollection.DeleteManyAsync(Builders<NewEmails>.Filter.Lt("EmailConversationId", messageconversationid));

            }
            else

            {
                // One unreaded mail only. 
                newEmailsCollection.DeleteOne(x => x.EmailConversationId == messageconversationid);

            }

        }

        // This is called timer to get unreaded mails
        public async Task<List<NewEmails>> GetUnreadedMails()
        {
            // Gets all of the unreaded mails to list and returns it.
            return newEmailsCollection.Find(_ => true).ToList();
        }





        // Get's comments.
        public async Task<List<Comments>> GetComments(string messageid)
        {
            // Get's comment collection by messageid and returns it.
            List<Comments> comments = commentsCollection.Find(x => x.message_id.Equals(messageid)).ToList();
            return comments;
        }


        // Function to send comments.
        public async void SendComment(string messageid, string message, string sender)
        {
            // Creates Comments variable and assings values to it.
            Comments comments = new Comments();
            comments.message_id = messageid;
            comments.message = message;
            comments.time = DateTime.Now;
            comments.sender = sender;

            // Insert's document to collection.
            commentsCollection.InsertOneAsync(comments);

        }


        // Gets Tickets that are assigned for spesific user.
        public async Task<List<Datamodel>> GetMessagesDBInProgressUserAsync(string name)
        {

            // Getting list of tickets by user name.
            List<Datamodel> listOfJobsInProgress = emailCollection.Find(x => x.handler.Equals(name)).ToList();
            return listOfJobsInProgress;


        }

        // Email functions
        //********************************************************************************'
        // Function to send email (forwarded)


        // Gets conversation which is showed in ui.
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
                                                          ItemSchema.DateTimeReceived, ItemSchema.TextBody, ItemSchema.IsFromMe, ItemSchema.DisplayTo, ItemSchema.DisplayCc, ItemSchema.MimeContent, EmailMessageSchema.From, EmailMessageSchema.ToRecipients, EmailMessageSchema.CcRecipients);

                // Identify the folders to ignore.
                Collection<FolderId> foldersToIgnore = new Collection<FolderId>() { WellKnownFolderName.DeletedItems, WellKnownFolderName.Drafts };

                // Request conversation items. This results in a call to the service.         
                response = service.GetConversationItems(convId, properties, null, foldersToIgnore, ConversationSortOrder.TreeOrderDescending);


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

        public async void SendForwardEmail(ItemId id, string text, List<string> receiversList, List<string> CCList)
        {
            FindItemsResults<Item> findResults;
            service.Url = new Uri(emailurl);
            int offSet = 0;
            // How many to get at function call (loads this many messages at one time).
            int pageSize = 5;

            // needed to use EWS.
            PropertySet propSet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.LastModifiedTime);
            EmailMessage message = EmailMessage.Bind(service, id, propSet);

            // Creates response by forward.
            ResponseMessage response = message.CreateForward();

            // Sets text for forward message.
            response.BodyPrefix = text;

            // Looping lists of CC/to recipients.
            foreach (string s in CCList)
            {
                response.CcRecipients.Add(s);

            }
            foreach (string s in receiversList)
            {
                response.ToRecipients.Add(s);

            }

            response.SendAndSaveCopy();

        }



        // Response function.
        public async void SendMail(ItemId id, string text, List<string> receiversList, List<string> CCList)
        {
            try
            {

            FindItemsResults<Item> findResults;
            service.Url = new Uri(emailurl);
            int offSet = 0;
            // How many to get at function call (loads this many messages at one time).
            int pageSize = 20;

            PropertySet propSet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.LastModifiedTime);
            EmailMessage message = EmailMessage.Bind(service, id, propSet);

            ResponseMessage response = message.CreateReply(false);
            response.BodyPrefix = text;
            // Loop to add Recipients to email.
            foreach (string s in CCList)
            {
                response.CcRecipients.Add(s);

            }
            foreach (string s in receiversList)
            {
                response.ToRecipients.Add(s);

            }

            response.SendAndSaveCopy();

			}
			catch (InvalidCastException e)
            {
				_logger.LogInformation(e.ToString());

			}

		}




			// Function used to get unreaded emails and insert them to database.
			public void GetEmails()
        {
            try
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
                NewEmails moro = new NewEmails();

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
                        emailModel.solutiondatetime = message.DateTimeReceived.AddDays(3);
                        emailModel.datetimereceived = message.DateTimeReceived;
                        emailModel.handler = "";
                        emailModel.solution = "";
                        emailModel.conversationid = message.ConversationId;
                        emailModel.colorCode = "green";
                        emailModel.status = "5";
                        // Sets email readed.
                        message.IsRead = true;

                        //Updates email state
                        message.Update(ConflictResolutionMode.AutoResolve);
                        try
                        {
                            //Inserts email to database collection
                            emailCollection.InsertOneAsync(emailModel);
                            // Reply message string





                            // While loop to check when message is readed, then sends reply mail.
                            while (message.IsRead == false)
                            {
                                message.Load();

                            }
                            if (message.IsRead == true)
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
                        if ((message.IsRead == false)) //if the current message is unread
                        {

                            message.Load();
                            message.IsRead = true;

                            if ((newEmailsCollection.CountDocuments(x => x.EmailConversationId == message.ConversationId)) >= 1)
                            {

                            }
                            else
                            {
                                moro.EmailConversationId = message.ConversationId.ToString();

                                newEmailsCollection.InsertOneAsync(moro);
                            }

                            message.Update(ConflictResolutionMode.AlwaysOverwrite);


                        }
                    }

                }

            }
            catch (InvalidCastException e)
            {
                _logger.LogInformation(e.ToString());
            }

        }



        // End of email functions
        //***********************************************************************
        public async Task<List<Datamodel>> getHistory()
        {

            //var filter = Builders<Datamodel>.Filter.Ne("handler", name);
            List<Datamodel> ListHistory = historyCollection.Find(x => x.status.Equals("1")).ToList();
            return ListHistory;


        }

        public async Task<Datamodel> GetTicketStatus(string messageid)
        {
            Datamodel email = new Datamodel();
            email = emailCollection.Find(x => x.message_id.Equals(messageid)).FirstOrDefault();
            return email;
        }
        public async Task<List<Datamodel>> GetMessagesDBInProgressAsync()
        {

            var filter = Builders<Datamodel>.Filter.Ne("handler", "");
            List<Datamodel> listOfJobsInProgress = emailCollection.Find(filter).ToList();
            return listOfJobsInProgress;


        }
        // Function used to get messages from database
        public async Task<List<Datamodel>> GetMessagesDbAsync()
        {

            List<Datamodel> listOfEmails = emailCollection.Find(x => x.handler.Equals("")).ToList();
            return listOfEmails;

        }
        public async Task<List<Datamodel>> GetTicketsInProgress()
        {
            List<Datamodel> listOfEmails = emailCollection.Find(x => x.handler != "").ToList();
            return listOfEmails;
        }
        public async void ChangeTicketCollection(string messageid, string status)
        {
            try
            {
                Datamodel ticket = emailCollection.Find(x => x.message_id == messageid).FirstOrDefault();

                ticket.status = status;
                ticket.solution = "";
                emailCollection.InsertOneAsync(ticket);
                historyCollection.DeleteOne(x => x.message_id == messageid);
            }
            catch (InvalidCastException e)
            {
                _logger.LogInformation(e.ToString());
            }
        }


        public async Task<Datamodel> GetTicketStatusFromHistory(string messageid)
        {
            Datamodel email = new Datamodel();
            email = historyCollection.Find(x => x.message_id.Equals(messageid)).FirstOrDefault();
            return email;
        }
        public async void AssingTicket(string name, string messageid)
        {

            // Try catch block to assign user object id to spesific ticket.
            try
            {

                var update = Builders<Datamodel>.Update.Set("handler", name).Set("status", "3");

                var filter = Builders<Datamodel>.Filter.Eq("message_id", messageid);
                Datamodel ticket = emailCollection.Find(x => x.message_id == messageid).FirstOrDefault();
                var options = new UpdateOptions { IsUpsert = true };
                emailCollection.UpdateOne(filter, update, options);
                // _logger.LogInformation(ticket.subject.ToString());
            }
            catch (InvalidCastException e)
            {
                _logger.LogInformation(e.ToString());
            }

        }


        public void SetTicketStatusHistory(string _value, string solvetext, string messageid)
        {
            Datamodel ticket = historyCollection.Find(x => x.message_id == messageid).FirstOrDefault();
            if (_value == "1")
            {

            }
            else
            {
                ticket.status = _value;
                ticket.solution = solvetext;
                emailCollection.InsertOneAsync(ticket);
                historyCollection.DeleteOne(x => x.message_id == messageid);

            }


        }


        public void SetTicketStatus(string _value, string solvetext, string messageid)
        {

            Datamodel ticket = emailCollection.Find(x => x.message_id == messageid).FirstOrDefault();
            if (_value == "1")
            {
                ticket.status = _value;
                ticket.solution = solvetext;
                historyCollection.InsertOneAsync(ticket);
                emailCollection.DeleteOne(x => x.message_id == messageid);
            }
            else
            {
                var update = Builders<Datamodel>.Update.Set("status", _value).Set("solution", solvetext);
                var filter = Builders<Datamodel>.Filter.Eq("message_id", messageid);
                var options = new UpdateOptions { IsUpsert = true };
                emailCollection.UpdateOne(filter, update, options);
            }



            //emailCollection.InsertOneAsync(emailModel);

        }



    }
    
}
