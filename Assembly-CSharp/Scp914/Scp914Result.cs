using System;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

namespace Scp914
{
	public readonly struct Scp914Result
	{
		public Scp914Result(ItemBase source)
		{
			this.SourceItem = source;
			this.SourcePickup = null;
			this.ResultingItems = null;
			this.ResultingPickups = null;
		}

		public Scp914Result(ItemBase sourceItem, ItemBase[] resultingItems, ItemPickupBase[] resultingPickups)
		{
			this.SourceItem = sourceItem;
			this.SourcePickup = null;
			this.ResultingItems = resultingItems;
			this.ResultingPickups = resultingPickups;
		}

		public Scp914Result(ItemBase source, ItemBase resultingItem, ItemPickupBase resultingPickup)
		{
			this.SourceItem = source;
			this.SourcePickup = null;
			this.ResultingItems = new ItemBase[] { resultingItem };
			this.ResultingPickups = new ItemPickupBase[] { resultingPickup };
		}

		public Scp914Result(ItemPickupBase source)
		{
			this.SourceItem = null;
			this.SourcePickup = source;
			this.ResultingItems = null;
			this.ResultingPickups = null;
		}

		public Scp914Result(ItemPickupBase sourcePickup, ItemBase[] resultingItems, ItemPickupBase[] resultingPickups)
		{
			this.SourceItem = null;
			this.SourcePickup = sourcePickup;
			this.ResultingItems = resultingItems;
			this.ResultingPickups = resultingPickups;
		}

		public Scp914Result(ItemPickupBase source, ItemBase resultingItem, ItemPickupBase resultingPickup)
		{
			this.SourceItem = null;
			this.SourcePickup = source;
			this.ResultingItems = new ItemBase[] { resultingItem };
			this.ResultingPickups = new ItemPickupBase[] { resultingPickup };
		}

		public readonly ItemBase SourceItem;

		public readonly ItemPickupBase SourcePickup;

		public readonly ItemBase[] ResultingItems;

		public readonly ItemPickupBase[] ResultingPickups;
	}
}
