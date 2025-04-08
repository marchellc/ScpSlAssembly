using System;
using Mirror;

namespace Hints
{
	public class ItemHintParameter : IdHintParameter
	{
		public static ItemHintParameter FromNetwork(NetworkReader reader)
		{
			ItemHintParameter itemHintParameter = new ItemHintParameter();
			itemHintParameter.Deserialize(reader);
			return itemHintParameter;
		}

		private ItemHintParameter()
		{
		}

		public ItemHintParameter(ItemType item)
			: base((byte)item)
		{
			if (item == ItemType.None)
			{
				throw new ArgumentException("Item cannot be none (no proper translation).", "item");
			}
		}
	}
}
