using System;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class DamageSubEffect : HypothermiaSubEffectBase
	{
		public override bool IsActive
		{
			get
			{
				return this._temperature.IsActive;
			}
		}

		internal override void UpdateEffect(float curExposure)
		{
			if (!NetworkServer.active || SpawnProtected.CheckPlayer(base.Hub))
			{
				return;
			}
			IHumeShieldedRole humeShieldedRole = base.Hub.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole != null && humeShieldedRole.HumeShieldModule.HsCurrent > 0f)
			{
				return;
			}
			this.DealDamage(this._temperature.CurTemperature);
		}

		private void DealDamage(float curTemp)
		{
			this._damageCounter += Mathf.Max(this._damageOverTemperature.Evaluate(curTemp) * Time.deltaTime, 0f);
			if (this._damageCounter < 1f)
			{
				return;
			}
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._damageCounter, DeathTranslations.Hypothermia, null));
			this._damageCounter = 0f;
		}

		private float _damageCounter;

		[SerializeField]
		private TemperatureSubEffect _temperature;

		[SerializeField]
		private AnimationCurve _damageOverTemperature;
	}
}
