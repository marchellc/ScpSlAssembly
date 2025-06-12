using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching;

public class Scp244SearchCompletor : ItemSearchCompletor
{
	public Scp244SearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
		: base(hub, targetPickup, targetItem, maxDistanceSquared)
	{
	}

	protected override bool ValidateAny()
	{
		if (base.ValidateAny() && base.TargetPickup is Scp244DeployablePickup scp244DeployablePickup)
		{
			return !scp244DeployablePickup.ModelDestroyed;
		}
		return false;
	}

	public override void Complete()
	{
		if (base.TargetPickup is Scp244DeployablePickup scp244DeployablePickup)
		{
			PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(base.Hub, base.TargetPickup));
			PlayerPickingUpItemEventArgs e = new PlayerPickingUpItemEventArgs(base.Hub, base.TargetPickup);
			PlayerEvents.OnPickingUpItem(e);
			if (e.IsAllowed)
			{
				ItemBase item = base.Hub.inventory.ServerAddItem(base.TargetPickup.Info.ItemId, ItemAddReason.PickedUp, base.TargetPickup.Info.Serial, base.TargetPickup);
				scp244DeployablePickup.State = Scp244State.PickedUp;
				base.CheckCategoryLimitHint();
				PlayerEvents.OnPickedUpItem(new PlayerPickedUpItemEventArgs(base.Hub, item));
			}
		}
	}
}
