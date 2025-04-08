using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Asphyxiated : TickingEffectBase, IStaminaModifier
	{
		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 0f;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._stamina = base.Hub.playerStats.GetModule<StaminaStat>();
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._stamina.CurValue = Mathf.Clamp01(this._stamina.CurValue - this.staminaDrainPerTick * 0.01f);
			if (this._stamina.CurValue <= 0f)
			{
				float num = this.healthDrainPerTick * RainbowTaste.CurrentMultiplier(base.Hub);
				base.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Asphyxiated, null));
			}
		}

		public float staminaDrainPerTick = 5f;

		public float healthDrainPerTick = 2f;

		private StaminaStat _stamina;
	}
}
