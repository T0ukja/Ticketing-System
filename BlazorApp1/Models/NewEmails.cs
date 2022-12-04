using Microsoft.Exchange.WebServices.Data;
using MongoDB.Bson;

namespace BlazorApp1.Models
{
	public class NewEmails
	{

		public ObjectId id { get; set; }
		public string EmailConversationId { get; set; }

	}
}
