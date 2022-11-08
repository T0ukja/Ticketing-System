using MongoDB.Bson;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlazorApp1.Models
{
    public class Datamodel
    {


        //  [BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
    public ObjectId id { get; set; }
    public string? subject { get; set; }
    public string sender { get; set; }
    public string attachment { get; set; }
    public string message_id { get; set; }
    public string datetimecreated { get; set; }
    public string datetimereceived { get; set; }
    public string handler { get; set; }
    public string status { get; set; }

    }
}
