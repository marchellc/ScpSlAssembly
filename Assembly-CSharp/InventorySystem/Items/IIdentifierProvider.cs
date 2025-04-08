using System;

namespace InventorySystem.Items
{
	public interface IIdentifierProvider
	{
		ItemIdentifier ItemId { get; }
	}
}
