﻿using MongoDB.Bson;

namespace BlazorApp1.Models
{
	public class Comments
	{
		public ObjectId id { get; set; }
	
		public string message_id { get; set; }
	
		public string message { get; set; }



	}
}
