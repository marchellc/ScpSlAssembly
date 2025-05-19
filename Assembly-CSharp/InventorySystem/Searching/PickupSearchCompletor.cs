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
		Hub = hub;
		TargetPickup = targetPickup;
		TargetItemType = targetPickup.Info.ItemId;
		MaxDistanceSqr = maxDistanceSquared;
	}

	protected bool ValidateDistance()
	{
		return (double)(TargetPickup.transform.position - Hub.transform.position).sqrMagnitude <= MaxDistanceSqr;
	}

	protected virtual bool ValidateAny()
	{
		if (Hub.roleManager.CurrentRole is IInventoryRole && !TargetPickup.Info.Locked && !Hub.inventory.IsDisarmed())
		{
			return !Hub.interCoordinator.AnyBlocker(BlockedInteraction.GrabItems);
		}
		return false;
	}

	public virtual bool ValidateStart()
	{
		PlayerSearchingPickupEventArgs playerSearchingPickupEventArgs = new PlayerSearchingPickupEventArgs(Hub, TargetPickup);
		PlayerEvents.OnSearchingPickup(playerSearchingPickupEventArgs);
		if (!playerSearchingPickupEventArgs.IsAllowed)
		{
			return false;
		}
		if (ValidateAny())
		{
			return ValidateDistance();
		}
		return false;
	}

	public virtual bool ValidateUpdate()
	{
		if (TargetPickup != null && ValidateAny())
		{
			return ValidateDistance();
		}
		return false;
	}

	public virtual void Complete()
	{
		PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(Hub, TargetPickup));
	}
}
