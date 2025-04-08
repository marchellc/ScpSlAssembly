using System;

namespace InventorySystem.Items.Usables
{
	public readonly struct CurrentlyUsedItem
	{
		public CurrentlyUsedItem(UsableItem item, ushort serial, float startTime)
		{
			this.Item = item;
			this.ItemSerial = serial;
			this.StartTime = startTime;
		}

		public static CurrentlyUsedItem None = new CurrentlyUsedItem(null, 0, 0f);

		public readonly UsableItem Item;

		public readonly ushort ItemSerial;

		public readonly float StartTime;
	}
}
