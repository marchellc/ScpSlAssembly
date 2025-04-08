using System;
using Footprinting;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class TimedGrenadePickup : CollisionDetectionPickup, IExplosionTrigger
	{
		private void Update()
		{
			if (!this._replaceNextFrame)
			{
				return;
			}
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(this.Info.ItemId, out itemBase))
			{
				return;
			}
			ThrowableItem throwableItem = itemBase as ThrowableItem;
			if (throwableItem == null)
			{
				return;
			}
			ThrownProjectile thrownProjectile = global::UnityEngine.Object.Instantiate<ThrownProjectile>(throwableItem.Projectile);
			PickupStandardPhysics pickupStandardPhysics = thrownProjectile.PhysicsModule as PickupStandardPhysics;
			if (pickupStandardPhysics != null)
			{
				PickupStandardPhysics pickupStandardPhysics2 = base.PhysicsModule as PickupStandardPhysics;
				if (pickupStandardPhysics2 != null)
				{
					Rigidbody rb = pickupStandardPhysics.Rb;
					Rigidbody rb2 = pickupStandardPhysics2.Rb;
					rb.position = rb2.position;
					rb.rotation = rb2.rotation;
					rb.velocity = rb2.velocity;
					rb.angularVelocity = rb2.angularVelocity;
				}
			}
			this.Info.Locked = true;
			thrownProjectile.NetworkInfo = this.Info;
			thrownProjectile.PreviousOwner = this._attacker;
			NetworkServer.Spawn(thrownProjectile.gameObject, null);
			thrownProjectile.ServerActivate();
			base.DestroySelf();
			this._replaceNextFrame = false;
		}

		public void OnExplosionDetected(Footprint attacker, Vector3 source, float range)
		{
			if (Vector3.Distance(base.transform.position, source) / range > 0.4f)
			{
				return;
			}
			if (Physics.Linecast(base.transform.position, source, ThrownProjectile.HitBlockerMask))
			{
				return;
			}
			this._replaceNextFrame = true;
			this._attacker = attacker;
		}

		public override bool Weaved()
		{
			return true;
		}

		private bool _replaceNextFrame;

		private Footprint _attacker;

		private const float ActivationRange = 0.4f;
	}
}
