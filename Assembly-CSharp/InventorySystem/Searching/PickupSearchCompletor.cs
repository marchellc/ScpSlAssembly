using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching;

public abstract class PickupSearchCompletor : ISearchCompletor
{
	public readonly ItemPickupBase TargetPickup;

	protected readonly ItemType TargetItemType;

	protected readonly double MaxDistanceSqr;

	public virtual bool AllowPickupUponEscape => true;

	public ReferenceHub Hub { get; }

	protected PickupSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, double maxDistanceSquared)
	{
		this.Hub = hub;
		this.TargetPickup = targetPickup;
		this.TargetItemType = targetPickup.Info.ItemId;
		this.MaxDistanceSqr = maxDistanceSquared;
	}

	protected bool ValidateDistance()
	{
		return (double)(this.TargetPickup.transform.position - this.Hub.transform.position).sqrMagnitude <= this.MaxDistanceSqr;
	}

	protected virtual bool ValidateAny()
	{
		if (this.Hub.roleManager.CurrentRole is IInventoryRole && !this.TargetPickup.Info.Locked && !this.Hub.inventory.IsDisarmed())
		{
			return !this.Hub.interCoordinator.AnyBlocker(BlockedInteraction.GrabItems);
		}
		return false;
	}

	public virtual bool ValidateStart()
	{
		PlayerSearchingPickupEventArgs e = new PlayerSearchingPickupEventArgs(this.Hub, this.TargetPickup);
		PlayerEvents.OnSearchingPickup(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		if (this.ValidateAny())
		{
			return this.ValidateDistance();
		}
		return false;
	}

	public virtual bool ValidateUpdate()
	{
		if (this.TargetPickup != null && this.ValidateAny())
		{
			return this.ValidateDistance();
		}
		return false;
	}

	public virtual void Complete()
	{
		PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(this.Hub, this.TargetPickup));
	}
}
