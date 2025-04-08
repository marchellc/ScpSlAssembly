using System;

namespace InventorySystem.Items
{
	public interface IDisarmingItem
	{
		bool AllowDisarming { get; }
	}
}
