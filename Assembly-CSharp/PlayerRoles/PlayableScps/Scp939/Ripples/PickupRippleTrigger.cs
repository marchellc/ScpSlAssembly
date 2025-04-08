using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class PickupRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			PickupRippleTrigger.ActiveInstances.Add(this);
			PickupRippleTrigger._anyInstances = true;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.RemoveSelf();
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteRelativePosition(this._syncPos);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			base.Player.Play(reader.ReadRelativePosition().Position, Color.red);
		}

		private void OnDestroy()
		{
			this.RemoveSelf();
		}

		private void RemoveSelf()
		{
			if (!PickupRippleTrigger.ActiveInstances.Remove(this))
			{
				return;
			}
			PickupRippleTrigger._anyInstances = PickupRippleTrigger.ActiveInstances.Count > 0;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ItemPickupBase.OnPickupAdded += PickupRippleTrigger.OnPickupAdded;
		}

		private static void OnPickupAdded(ItemPickupBase ipb)
		{
			CollisionDetectionPickup cdp = ipb as CollisionDetectionPickup;
			if (cdp == null)
			{
				return;
			}
			if (!NetworkServer.active)
			{
				return;
			}
			cdp.OnCollided += delegate(Collision col)
			{
				PickupRippleTrigger.OnCollided(cdp, col);
			};
		}

		private static void OnCollided(CollisionDetectionPickup cdp, Collision collision)
		{
			if (!PickupRippleTrigger._anyInstances || !NetworkServer.active)
			{
				return;
			}
			float sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
			if (sqrMagnitude < 8.5f || collision.contactCount == 0)
			{
				return;
			}
			float num = Mathf.Max(4f, cdp.Info.WeightKg * 0.75f);
			num = Mathf.Max(num, cdp.GetRangeOfCollisionVelocity(sqrMagnitude));
			foreach (PickupRippleTrigger pickupRippleTrigger in PickupRippleTrigger.ActiveInstances)
			{
				if (pickupRippleTrigger._rateLimiter.RateReady)
				{
					Vector3 point = collision.GetContact(0).point;
					if ((point - pickupRippleTrigger.CastRole.FpcModule.Position).sqrMagnitude < num * num)
					{
						pickupRippleTrigger._rateLimiter.RegisterInput();
						pickupRippleTrigger._syncPos = new RelativePosition(point);
						pickupRippleTrigger.ServerSendRpcToObservers();
					}
				}
			}
		}

		private const float MinVelSqr = 8.5f;

		private const float SoundRangeMin = 4f;

		private const float SoundRangeKg = 0.75f;

		private RelativePosition _syncPos;

		private readonly RateLimiter _rateLimiter = new RateLimiter(0f, 8, 0.2f);

		private static bool _anyInstances;

		private static readonly HashSet<PickupRippleTrigger> ActiveInstances = new HashSet<PickupRippleTrigger>();
	}
}
