using System;
using Mirror;

namespace InventorySystem.Searching
{
	public static class SearchRequestFunctions
	{
		public static void Serialize(this NetworkWriter writer, SearchRequest value)
		{
			value.Serialize(writer);
		}

		public static SearchRequest Deserialize(this NetworkReader reader)
		{
			SearchRequest searchRequest = default(SearchRequest);
			searchRequest.Deserialize(reader);
			return searchRequest;
		}
	}
}
