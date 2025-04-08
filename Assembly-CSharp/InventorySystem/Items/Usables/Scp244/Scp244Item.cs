using System;
using Footprinting;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public class Scp244Item : UsableItem, ICustomSearchCompletorItem
	{
		public override void ServerOnUsingCompleted()
		{
			this._primed = true;
			base.OwnerInventory.ServerDropItem(base.ItemSerial);
		}

		public override void OnUsingCancelled()
		{
			base.OnUsingCancelled();
			this._primed = false;
		}

		public override ItemPickupBase ServerDropItem(bool spawn)
		{
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo
			{
				ItemId = this.ItemTypeId,
				Serial = base.ItemSerial,
				WeightKg = this.Weight
			};
			ItemPickupBase itemPickupBase = base.OwnerInventory.ServerCreatePickup(this, new PickupSyncInfo?(pickupSyncInfo), spawn, delegate(ItemPickupBase ipb)
			{
				ipb.PreviousOwner = new Footprint(base.Owner);
				Scp244DeployablePickup scp244DeployablePickup = ipb as Scp244DeployablePickup;
				if (scp244DeployablePickup != null)
				{
					scp244DeployablePickup.State = (this._primed ? Scp244State.Active : Scp244State.Idle);
				}
				Transform transform = base.Owner.transform;
				ipb.transform.SetPositionAndRotation(transform.position - Vector3.up * 0.72f, transform.rotation);
			});
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, itemPickupBase);
			return itemPickupBase;
		}

		public SearchCompletor GetCustomSearchCompletor(ReferenceHub hub, ItemPickupBase ipb, ItemBase ib, double disSqrt)
		{
			return new Scp244SearchCompletor(hub, ipb, ib, disSqrt);
		}

		private bool _primed;

		private const float DropHeightOffset = 0.72f;
	}
}
