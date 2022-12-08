using BC = BCrypt.Net.BCrypt;

using MongoDB.Driver;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using static MongoDB.Driver.WriteConcern;
using MongoDB.Bson;

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

        public async void SetNewPassword(string newpassword, ObjectId id )
        {
            string mySalt = BC.GenerateSalt(10);
            string myHash = BC.HashPassword(newpassword, mySalt);
            var update = Builders<LoginModel>.Update.Set("password", myHash);
            var filter = Builders<LoginModel>.Filter.Eq("id", id);
            var options = new UpdateOptions { IsUpsert = true };
            userNameCollection.UpdateOne(filter, update, options);
        }
		public bool ChangePassword(string oldpassword, string newpassword, string name)
		{
            var DBuserLoginData = userNameCollection.Find(m => m.username == name).FirstOrDefault();

            bool doesPasswordMatch = BC.Verify(oldpassword, DBuserLoginData.password);

            if (doesPasswordMatch)
            {
                string mySalt = BC.GenerateSalt(10);
                string myHash = BC.HashPassword(newpassword, mySalt);
                var update = Builders<LoginModel>.Update.Set("password", myHash);
                var filter = Builders<LoginModel>.Filter.Eq("username", name);
                var options = new UpdateOptions { IsUpsert = true };
                userNameCollection.UpdateOne(filter, update, options);

                return true;
            }

            else
            {
                return false;
            }

        }

        public async void DeleteUser(ObjectId id)
        {
            userNameCollection.DeleteOne(x => x.id == id);
        }
        public bool CheckIfUserExists(string name)
        {

			if (userNameCollection.Find(x => x.username == name).Any())
            {
                return true;
            }
            return false;
        }
        public async Task <List<LoginModel>> UserList()
        {
            

            return userNameCollection.Find(_ => true).ToList();
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

