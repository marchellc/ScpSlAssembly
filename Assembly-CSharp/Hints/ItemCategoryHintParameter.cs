using System;
using Mirror;

namespace Hints
{
	public class ItemCategoryHintParameter : IdHintParameter
	{
		public static ItemCategoryHintParameter FromNetwork(NetworkReader reader)
		{
			ItemCategoryHintParameter itemCategoryHintParameter = new ItemCategoryHintParameter();
			itemCategoryHintParameter.Deserialize(reader);
			return itemCategoryHintParameter;
		}

		private ItemCategoryHintParameter()
		{
		}

		public ItemCategoryHintParameter(ItemCategory category)
		{
			if (category == ItemCategory.None)
			{
				throw new ArgumentException("Item category cannot be none (no proper translation).", "category");
			}
			base..ctor((byte)category);
		}
	}
}
