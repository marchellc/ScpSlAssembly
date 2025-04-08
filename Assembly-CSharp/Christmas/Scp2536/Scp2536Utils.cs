using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;

namespace Christmas.Scp2536
{
	public static class Scp2536Utils
	{
		public static void GrantAmmoReward(this ItemBase reward)
		{
			if (!(reward.Owner == null))
			{
				Firearm firearm = reward as Firearm;
				if (firearm != null)
				{
					IPrimaryAmmoContainerModule primaryAmmoContainerModule;
					if (!firearm.TryGetModule(out primaryAmmoContainerModule, true))
					{
						return;
					}
					reward.OwnerInventory.ServerAddAmmo(primaryAmmoContainerModule.AmmoType, primaryAmmoContainerModule.AmmoMax);
					return;
				}
			}
		}
	}
}
