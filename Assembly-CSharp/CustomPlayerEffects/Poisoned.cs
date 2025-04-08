using System;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Poisoned : TickingEffectBase, IHealableEffect, IPulseEffect
	{
		public void ExecutePulse()
		{
		}

		public bool IsHealable(ItemType it)
		{
			return it == ItemType.SCP500;
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this.damagePerTick, DeathTranslations.Poisoned, null));
			base.Hub.playerEffectsController.ServerSendPulse<Poisoned>();
			this.damagePerTick *= this.multPerTick;
			this.damagePerTick = Mathf.Clamp(this.damagePerTick, this.minDamage, this.maxDamage);
		}

		protected override void Enabled()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.damagePerTick = this.minDamage;
		}

		private float damagePerTick = 2f;

		private readonly float minDamage = 2f;

		private readonly float maxDamage = 20f;

		private readonly float multPerTick = 2f;
	}
}
