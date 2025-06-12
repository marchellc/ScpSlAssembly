using Footprinting;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244;

public class Scp244Item : UsableItem
{
	private bool _primed;

	private const float DropHeightOffset = 0.72f;

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
		PickupSyncInfo value = new PickupSyncInfo
		{
			ItemId = base.ItemTypeId,
			Serial = base.ItemSerial,
			WeightKg = this.Weight
		};
		ItemPickupBase itemPickupBase = base.OwnerInventory.ServerCreatePickup(this, value, spawn, delegate(ItemPickupBase ipb)
		{
			ipb.PreviousOwner = new Footprint(base.Owner);
			if (ipb is Scp244DeployablePickup scp244DeployablePickup)
			{
				scp244DeployablePickup.State = (this._primed ? Scp244State.Active : Scp244State.Idle);
			}
			Transform transform = base.Owner.transform;
			ipb.transform.SetPositionAndRotation(transform.position - Vector3.up * 0.72f, transform.rotation);
		});
		base.OwnerInventory.ServerRemoveItem(base.ItemSerial, itemPickupBase);
		return itemPickupBase;
	}
}
