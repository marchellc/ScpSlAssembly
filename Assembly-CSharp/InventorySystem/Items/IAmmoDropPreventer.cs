using System;

namespace InventorySystem.Items
{
	public interface IAmmoDropPreventer
	{
		bool ValidateAmmoDrop(ItemType id);

		public static bool CanDropAmmo(ReferenceHub hub, ItemType id)
		{
			ItemBase curInstance = hub.inventory.CurInstance;
			IAmmoDropPreventer ammoDropPreventer = curInstance as IAmmoDropPreventer;
			return ammoDropPreventer == null || curInstance == null || ammoDropPreventer.ValidateAmmoDrop(id);
		}
	}
}
