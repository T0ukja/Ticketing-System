using BC = BCrypt.Net.BCrypt;

using MongoDB.Driver;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;


namespace BlazorApp1.Data
{
  
    public class Login
    {
        // Variable declares for MongoDB database.
        private readonly IMongoCollection<LoginModel> userNameCollection;
        // Uncomment if wanna log data.
        private readonly ILogger _logger;
        private MongoClient mongoClient { get; set; }
        private readonly IMongoDatabase mongoDatabase;
        public Login(IOptions<Settingsmodel_login> settingsmodel  ,ILogger<Login> logger)
        {
            _logger = logger;
   
            // Settings model is used as class to read Mongodb
            // Settings Mongodb values from appsettings.json
            mongoClient = new MongoClient(
            settingsmodel.Value.ConnectionString);

            mongoDatabase = mongoClient.GetDatabase(
                settingsmodel.Value.DatabaseName);

            userNameCollection = mongoDatabase.GetCollection<LoginModel>(
                settingsmodel.Value.CollectionName);

        }


     
        public LoginModel CheckUserLogin(string username, string password)
        {
            // If user exists.
            if (userNameCollection.Count(m => m.username == username).Equals(1))
            {
                // Gets data from database.
                var DBuserLoginData = userNameCollection.Find(m => m.username == username).FirstOrDefault();
                // Check if database hashed/salted password match
                bool doesPasswordMatch = BC.Verify(password, DBuserLoginData.password);
                if (doesPasswordMatch == true)
                {
                    // Created loginmodel for adding data and return it to login view.
                    LoginModel loggedaccount = new LoginModel();
                    loggedaccount.username = DBuserLoginData.username;
                    loggedaccount.Role = DBuserLoginData.Role;

                    return loggedaccount;
                  
                }

                else
                {
                   
                    return null;

                }


            }
            else
            {
                return null;

            }
        }


            // Creating new user.
            public async Task CreateNewUser(string username, string password, string type)
        {
            LoginModel loginmodel = new LoginModel();
            string mySalt = BC.GenerateSalt(10);
            string myHash = BC.HashPassword(password, mySalt);
            loginmodel.username = username;
            loginmodel.password = myHash;
            loginmodel.Role = type;
            await userNameCollection.InsertOneAsync(loginmodel);



        }
    }


}

