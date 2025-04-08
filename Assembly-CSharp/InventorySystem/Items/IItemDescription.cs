using System;

namespace InventorySystem.Items
{
	public interface IItemDescription : IItemNametag
	{
		string Description { get; }
	}
}
