using System;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching
{
	public class Scp244SearchCompletor : ItemSearchCompletor
	{
		public Scp244SearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
		}

		protected override bool ValidateAny()
		{
			if (base.ValidateAny())
			{
				Scp244DeployablePickup scp244DeployablePickup = this.TargetPickup as Scp244DeployablePickup;
				if (scp244DeployablePickup != null)
				{
					return !scp244DeployablePickup.ModelDestroyed;
				}
			}
			return false;
		}

		public override void Complete()
		{
			Scp244DeployablePickup scp244DeployablePickup = this.TargetPickup as Scp244DeployablePickup;
			if (scp244DeployablePickup == null)
			{
				return;
			}
			PlayerPickingUpItemEventArgs playerPickingUpItemEventArgs = new PlayerPickingUpItemEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnPickingUpItem(playerPickingUpItemEventArgs);
			if (!playerPickingUpItemEventArgs.IsAllowed)
			{
				return;
			}
			ItemBase itemBase = this.Hub.inventory.ServerAddItem(this.TargetPickup.Info.ItemId, ItemAddReason.PickedUp, this.TargetPickup.Info.Serial, this.TargetPickup);
			scp244DeployablePickup.State = Scp244State.PickedUp;
			base.CheckCategoryLimitHint();
			PlayerEvents.OnPickedUpItem(new PlayerPickedUpItemEventArgs(this.Hub, itemBase));
		}
	}
}
