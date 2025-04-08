using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;

namespace CustomPlayerEffects
{
	public class Corroding : TickingEffectBase, IStaminaModifier
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active || this.AttackerHub == null || Vitality.CheckPlayer(base.Hub))
			{
				return;
			}
			base.Hub.playerStats.DealDamage(new ScpDamageHandler(this.AttackerHub, 2.1f, DeathTranslations.PocketDecay));
			base.Hub.playerStats.GetModule<StaminaStat>().CurValue -= 0.024999999f;
		}

		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 0f;
			}
		}

		private const float DamagePerTick = 2.1f;

		private const float StaminaDrainPercentage = 2.5f;

		public ReferenceHub AttackerHub;
	}
}
