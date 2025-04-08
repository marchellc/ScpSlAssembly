using System;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using UnityEngine;

namespace Scp914.Processors
{
	public class Scp244ItemProcessor : UsableItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase ipb)
		{
			Scp244DeployablePickup scp244DeployablePickup = ipb as Scp244DeployablePickup;
			if (scp244DeployablePickup == null || !scp244DeployablePickup.ModelDestroyed)
			{
				return base.UpgradePickup(setting, ipb);
			}
			return new Scp914Result(ipb, null, ipb);
		}

		protected override void HandleOldPickup(ItemPickupBase ipb, Vector3 newPos)
		{
			Scp244DeployablePickup scp244DeployablePickup = ipb as Scp244DeployablePickup;
			if (scp244DeployablePickup != null && scp244DeployablePickup.State == Scp244State.Active)
			{
				scp244DeployablePickup.State = Scp244State.PickedUp;
				return;
			}
			base.HandleOldPickup(ipb, newPos);
		}

		protected override void HandleNone(ItemPickupBase ipb, Vector3 newPos)
		{
			Scp244DeployablePickup scp244DeployablePickup = ipb as Scp244DeployablePickup;
			if (scp244DeployablePickup == null)
			{
				base.HandleNone(ipb, newPos);
				return;
			}
			if (scp244DeployablePickup.ModelDestroyed)
			{
				return;
			}
			scp244DeployablePickup.State = Scp244State.Destroyed;
		}
	}
}
