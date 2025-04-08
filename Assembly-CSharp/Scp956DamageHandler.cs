using System;
using InventorySystem;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

public class Scp956DamageHandler : StandardDamageHandler
{
	public override string ServerLogsText
	{
		get
		{
			return "Died to SCP-956";
		}
	}

	public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
	{
		get
		{
			return null;
		}
	}

	public Scp956DamageHandler(Vector3 direction)
	{
		this._velocity = (direction * 3f + Vector3.up) * 9f;
		this.Damage = -1f;
	}

	public override float Damage { get; internal set; }

	public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
		if (handlerOutput != DamageHandlerBase.HandlerOutput.Death)
		{
			return handlerOutput;
		}
		Scp330Bag scp330Bag;
		if (!InventoryItemLoader.TryGetItem<Scp330Bag>(ItemType.SCP330, out scp330Bag))
		{
			return handlerOutput;
		}
		int num = ((global::UnityEngine.Random.value < 0.1f) ? 1 : 0);
		for (int i = 0; i < 20; i++)
		{
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(ItemType.SCP330, scp330Bag.Weight, 0, false);
			Scp330Pickup scp330Pickup = ply.inventory.ServerCreatePickup(scp330Bag, new PickupSyncInfo?(pickupSyncInfo), true, null) as Scp330Pickup;
			if (!(scp330Pickup == null))
			{
				CandyKindID candyKindID = ((num-- > 0) ? CandyKindID.Pink : ((CandyKindID)global::UnityEngine.Random.Range(1, 7)));
				scp330Pickup.StoredCandies.Add(candyKindID);
				scp330Pickup.NetworkExposedCandy = candyKindID;
				Rigidbody rigidbody;
				if (scp330Pickup.TryGetComponent<Rigidbody>(out rigidbody))
				{
					rigidbody.velocity = this._velocity;
				}
			}
		}
		return handlerOutput;
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		this.StartVelocity = this._velocity;
		base.WriteAdditionalData(writer);
	}

	private readonly Vector3 _velocity;
}
