using System;

namespace InventorySystem.Items.Firearms.ShotEvents
{
	public abstract class ShotEvent
	{
		public ShotEvent(ItemIdentifier shotFirearm)
		{
			this.ItemId = shotFirearm;
		}

		public readonly ItemIdentifier ItemId;
	}
}
