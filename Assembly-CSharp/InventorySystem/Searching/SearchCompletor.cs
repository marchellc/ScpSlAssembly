using System;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching
{
	public abstract class SearchCompletor
	{
		public static SearchCompletor FromPickup(SearchCoordinator coordinator, ItemPickupBase targetPickup, double maxDistanceSquared)
		{
			ReferenceHub hub = coordinator.Hub;
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(targetPickup.Info.ItemId, out itemBase))
			{
				return null;
			}
			ICustomSearchCompletorItem customSearchCompletorItem = itemBase as ICustomSearchCompletorItem;
			if (customSearchCompletorItem == null)
			{
				return new ItemSearchCompletor(hub, targetPickup, itemBase, maxDistanceSquared);
			}
			return customSearchCompletorItem.GetCustomSearchCompletor(hub, targetPickup, itemBase, maxDistanceSquared);
		}

		public virtual bool AllowPickupUponEscape
		{
			get
			{
				return true;
			}
		}

		protected SearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
		{
			this.Hub = hub;
			this.TargetPickup = targetPickup;
			this.TargetItem = targetItem;
			this.MaxDistanceSqr = maxDistanceSquared;
		}

		protected bool ValidateDistance()
		{
			return (double)(this.TargetPickup.transform.position - this.Hub.transform.position).sqrMagnitude <= this.MaxDistanceSqr;
		}

		protected virtual bool ValidateAny()
		{
			return this.Hub.roleManager.CurrentRole is IInventoryRole && !this.TargetPickup.Info.Locked && !this.Hub.inventory.IsDisarmed() && !this.Hub.interCoordinator.AnyBlocker(BlockedInteraction.GrabItems);
		}

		public virtual bool ValidateStart()
		{
			PlayerSearchingPickupEventArgs playerSearchingPickupEventArgs = new PlayerSearchingPickupEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnSearchingPickup(playerSearchingPickupEventArgs);
			return playerSearchingPickupEventArgs.IsAllowed && this.ValidateAny() && this.ValidateDistance();
		}

		public virtual bool ValidateUpdate()
		{
			return this.TargetPickup != null && this.ValidateAny() && this.ValidateDistance();
		}

		public abstract void Complete();

		public readonly ReferenceHub Hub;

		public readonly ItemPickupBase TargetPickup;

		protected readonly ItemBase TargetItem;

		protected readonly double MaxDistanceSqr;
	}
}
