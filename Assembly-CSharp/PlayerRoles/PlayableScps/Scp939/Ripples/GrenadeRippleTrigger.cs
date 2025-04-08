using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class GrenadeRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			ThrownProjectile.OnProjectileSpawned += this.OnProjectileSpawned;
			ItemPickupBase.OnPickupDestroyed += this.OnPickupDestroyed;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			ThrownProjectile.OnProjectileSpawned -= this.OnProjectileSpawned;
			ItemPickupBase.OnPickupDestroyed -= this.OnPickupDestroyed;
		}

		private void OnProjectileSpawned(ThrownProjectile tp)
		{
			if (tp.Info.ItemId != ItemType.GrenadeHE)
			{
				return;
			}
			this._trackedGrenades.Add(tp, new GrenadeRippleTrigger.ThrownGrenadeHandler(this));
		}

		private void OnPickupDestroyed(ItemPickupBase ipb)
		{
			ThrownProjectile thrownProjectile = ipb as ThrownProjectile;
			if (thrownProjectile == null)
			{
				return;
			}
			this._trackedGrenades.Remove(thrownProjectile);
		}

		private void Update()
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			foreach (KeyValuePair<ThrownProjectile, GrenadeRippleTrigger.ThrownGrenadeHandler> keyValuePair in this._trackedGrenades)
			{
				if (keyValuePair.Value.UpdateSound())
				{
					base.PlayInRangeSqr(keyValuePair.Key.Position, this._audibleRangeSqr, Color.red);
				}
			}
		}

		[SerializeField]
		private float[] _rippleTimes;

		[SerializeField]
		private float _audibleRangeSqr;

		private readonly Dictionary<ThrownProjectile, GrenadeRippleTrigger.ThrownGrenadeHandler> _trackedGrenades = new Dictionary<ThrownProjectile, GrenadeRippleTrigger.ThrownGrenadeHandler>();

		private class ThrownGrenadeHandler
		{
			public ThrownGrenadeHandler(GrenadeRippleTrigger trigger)
			{
				this._tr = trigger;
				this._startTime = Time.timeSinceLevelLoad;
			}

			public bool UpdateSound()
			{
				if (this._nextTime >= this._tr._rippleTimes.Length)
				{
					return false;
				}
				float num = Time.timeSinceLevelLoad - this._startTime;
				if (this._tr._rippleTimes[this._nextTime] > num)
				{
					return false;
				}
				this._nextTime++;
				while (this.UpdateSound())
				{
				}
				return true;
			}

			private readonly GrenadeRippleTrigger _tr;

			private readonly float _startTime;

			private int _nextTime;
		}
	}
}
