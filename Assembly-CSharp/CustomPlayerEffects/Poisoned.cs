using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class Poisoned : TickingEffectBase, IHealableEffect, IPulseEffect
{
	private float damagePerTick = 2f;

	private readonly float minDamage = 2f;

	private readonly float maxDamage = 20f;

	private readonly float multPerTick = 2f;

	public void ExecutePulse()
	{
	}

	public bool IsHealable(ItemType it)
	{
		return it == ItemType.SCP500;
	}

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this.damagePerTick, DeathTranslations.Poisoned));
			base.Hub.playerEffectsController.ServerSendPulse<Poisoned>();
			this.damagePerTick *= this.multPerTick;
			this.damagePerTick = Mathf.Clamp(this.damagePerTick, this.minDamage, this.maxDamage);
		}
	}

	protected override void Enabled()
	{
		if (NetworkServer.active)
		{
			this.damagePerTick = this.minDamage;
		}
	}
}
