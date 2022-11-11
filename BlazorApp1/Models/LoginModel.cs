using MongoDB.Bson;

namespace BlazorApp1.Models
{

    // Model for login.
    public class LoginModel
    {
        public ObjectId id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string Role { get; set; }

    }
}
