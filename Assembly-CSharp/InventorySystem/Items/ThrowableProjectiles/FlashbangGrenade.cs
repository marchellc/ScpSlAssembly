using System;
using CustomPlayerEffects;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class FlashbangGrenade : EffectGrenade
	{
		public override void PlayExplosionEffects(Vector3 pos)
		{
			base.PlayExplosionEffects(pos);
			if (!MainCameraController.InstanceActive)
			{
				return;
			}
			float num = Vector3.Distance(MainCameraController.CurrentCamera.position, pos);
			float num2 = this._shakeOverDistance.Evaluate(num);
			ExplosionCameraShake.singleton.Shake(num2);
		}

		public override bool ServerFuseEnd()
		{
			if (!base.ServerFuseEnd())
			{
				return false;
			}
			float time = this._blindingOverDistance.keys[this._blindingOverDistance.length - 1].time;
			float num = time * time;
			this._hitPlayerCount = 0;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if ((base.transform.position - referenceHub.transform.position).sqrMagnitude <= num && !(referenceHub == this.PreviousOwner.Hub) && HitboxIdentity.IsDamageable(this.PreviousOwner.Role, referenceHub.GetRoleId()))
				{
					this.ProcessPlayer(referenceHub);
				}
			}
			if (this._hitPlayerCount > 0)
			{
				Hitmarker.SendHitmarkerDirectly(this.PreviousOwner.Hub, (float)this._hitPlayerCount, true);
			}
			ServerEvents.OnProjectileExploded(new ProjectileExplodedEventArgs(this, this.PreviousOwner.Hub, base.transform.position));
			return true;
		}

		private void ProcessPlayer(ReferenceHub hub)
		{
			if (Physics.Linecast(base.transform.position, hub.PlayerCameraReference.position, this.BlindingMask))
			{
				return;
			}
			Vector3 vector = base.transform.position - hub.PlayerCameraReference.position;
			float num = vector.magnitude;
			if (hub.transform.position.y > 900f)
			{
				num /= this._surfaceZoneDistanceIntensifier;
			}
			bool flag = Vector3.Dot(hub.PlayerCameraReference.forward, vector.normalized) >= 0.5f;
			float num2 = (flag ? this._blindingOverDistance.Evaluate(num) : this._turnedAwayBlindingDistance.Evaluate(num));
			float num3 = (flag ? num2 : this._turnedAwayDeafenDurationOverDistance.Evaluate(num));
			float num4 = (flag ? this._deafenDurationOverDistance.Evaluate(num) : (num3 * this.BlindTime));
			if (num4 > this._minimalEffectDuration)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Throwable, hub.LoggedNameFromRefHub() + " has been deafened by " + this.PreviousOwner.LoggedNameFromFootprint() + " using a flashbang grenade.", ServerLogs.ServerLogType.GameEvent, false);
				hub.playerEffectsController.EnableEffect<Deafened>(num4, true);
			}
			if (num2 > this._minimalEffectDuration)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Throwable, hub.LoggedNameFromRefHub() + " has been flashed by " + this.PreviousOwner.LoggedNameFromFootprint() + " using a flashbang grenade.", ServerLogs.ServerLogType.GameEvent, false);
				this._hitPlayerCount++;
				hub.playerEffectsController.EnableEffect<Flashed>(num2 * this.BlindTime, true);
			}
			if (num <= 10f)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Throwable, hub.LoggedNameFromRefHub() + " has been blinded by " + this.PreviousOwner.LoggedNameFromFootprint() + " using a flashbang grenade.", ServerLogs.ServerLogType.GameEvent, false);
				hub.playerEffectsController.EnableEffect<Blurred>(num3 * this.BlindTime + this._additionalBlurDuration * num3, true);
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public LayerMask BlindingMask;

		public float BlindTime;

		[SerializeField]
		private AnimationCurve _blindingOverDistance;

		[SerializeField]
		private AnimationCurve _turnedAwayBlindingDistance;

		[SerializeField]
		private AnimationCurve _blindingOverDot;

		[SerializeField]
		private AnimationCurve _deafenDurationOverDistance;

		[SerializeField]
		private AnimationCurve _turnedAwayDeafenDurationOverDistance;

		[SerializeField]
		private AnimationCurve _shakeOverDistance;

		[SerializeField]
		private float _surfaceZoneDistanceIntensifier;

		[SerializeField]
		private float _additionalBlurDuration;

		[SerializeField]
		private float _minimalEffectDuration;

		private int _hitPlayerCount;
	}
}
