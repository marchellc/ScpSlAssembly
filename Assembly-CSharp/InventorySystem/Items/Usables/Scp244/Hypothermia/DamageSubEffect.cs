using CustomPlayerEffects;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class DamageSubEffect : HypothermiaSubEffectBase
{
	private float _damageCounter;

	[SerializeField]
	private TemperatureSubEffect _temperature;

	[SerializeField]
	private AnimationCurve _damageOverTemperature;

	public override bool IsActive => this._temperature.IsActive;

	internal override void UpdateEffect(float curExposure)
	{
		if (NetworkServer.active && !SpawnProtected.CheckPlayer(base.Hub) && (!(base.Hub.roleManager.CurrentRole is IHumeShieldedRole humeShieldedRole) || !(humeShieldedRole.HumeShieldModule.HsCurrent > 0f)))
		{
			this.DealDamage(this._temperature.CurTemperature);
		}
	}

	private void DealDamage(float curTemp)
	{
		this._damageCounter += Mathf.Max(this._damageOverTemperature.Evaluate(curTemp) * Time.deltaTime, 0f);
		if (!(this._damageCounter < 1f))
		{
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._damageCounter, DeathTranslations.Hypothermia));
			this._damageCounter = 0f;
		}
	}
}
