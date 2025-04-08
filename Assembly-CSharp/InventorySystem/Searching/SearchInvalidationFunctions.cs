using System;
using Mirror;

namespace InventorySystem.Searching
{
	public static class SearchInvalidationFunctions
	{
		public static void Serialize(this NetworkWriter writer, SearchInvalidation value)
		{
			value.Serialize(writer);
		}

		public static SearchInvalidation Deserialize(this NetworkReader reader)
		{
			SearchInvalidation searchInvalidation = default(SearchInvalidation);
			searchInvalidation.Deserialize(reader);
			return searchInvalidation;
		}
	}
}
