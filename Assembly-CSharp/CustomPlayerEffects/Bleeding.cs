using System;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Bleeding : TickingEffectBase, IPulseEffect, IHealableEffect
	{
		public void ExecutePulse()
		{
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			float num = this.damagePerTick * RainbowTaste.CurrentMultiplier(base.Hub);
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Bleeding, null));
			base.Hub.playerEffectsController.ServerSendPulse<Bleeding>();
			this.damagePerTick *= this.multPerTick;
			this.damagePerTick = Mathf.Clamp(this.damagePerTick, this.minDamage, this.maxDamage);
		}

		protected override void Enabled()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.damagePerTick = this.maxDamage;
		}

		public bool IsHealable(ItemType it)
		{
			if (it == ItemType.SCP500)
			{
				this.damagePerTick = this.minDamage;
			}
			return it == ItemType.Medkit;
		}

		public float minDamage = 2f;

		public float maxDamage = 20f;

		public float multPerTick = 0.5f;

		public float damagePerTick = 20f;
	}
}
