using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class Bleeding : TickingEffectBase, IPulseEffect, IHealableEffect
{
	public float minDamage = 2f;

	public float maxDamage = 20f;

	public float multPerTick = 0.5f;

	public float damagePerTick = 20f;

	public void ExecutePulse()
	{
	}

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			float damage = this.damagePerTick * RainbowTaste.CurrentMultiplier(base.Hub);
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Bleeding));
			base.Hub.playerEffectsController.ServerSendPulse<Bleeding>();
			this.damagePerTick *= this.multPerTick;
			this.damagePerTick = Mathf.Clamp(this.damagePerTick, this.minDamage, this.maxDamage);
		}
	}

	protected override void Enabled()
	{
		if (NetworkServer.active)
		{
			this.damagePerTick = this.maxDamage;
		}
	}

	public bool IsHealable(ItemType it)
	{
		if (it == ItemType.SCP500)
		{
			this.damagePerTick = this.minDamage;
		}
		return it == ItemType.Medkit;
	}
}
