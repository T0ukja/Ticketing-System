namespace BlazorApp1.Models
{
    // Class to read configurations for MongoDB
    public class Settingsmodel_login
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string CollectionName { get; set; } = null!;
    }
}
