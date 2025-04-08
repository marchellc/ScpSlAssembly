using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects
{
	public class Scp207 : CokeBase<Scp207Stack>, ISpectatorDataPlayerEffect, IStaminaModifier, ICustomRADisplay
	{
		public override Dictionary<PlayerMovementState, float> StateMultipliers { get; }

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Mixed;
			}
		}

		public override float MovementSpeedMultiplier
		{
			get
			{
				return base.CurrentStack.SpeedMultiplier;
			}
		}

		public string DisplayName
		{
			get
			{
				return "SCP-207";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

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
				return 0f;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = ((base.Intensity > 1) ? string.Format("SCP-207 (x{0})", base.Intensity) : "SCP-207");
			return true;
		}

		protected override void Enabled()
		{
			base.Enabled();
			if (!NetworkServer.active)
			{
				return;
			}
			StaminaStat staminaStat;
			if (base.Hub.playerStats.TryGetModule<StaminaStat>(out staminaStat))
			{
				staminaStat.CurValue = staminaStat.MaxValue;
			}
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (Vitality.CheckPlayer(base.Hub))
			{
				return;
			}
			float num = base.CurrentStack.DamageAmount * base.GetMovementStateMultiplier();
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Scp207, null));
		}

		public Scp207()
		{
			Dictionary<PlayerMovementState, float> dictionary = new Dictionary<PlayerMovementState, float>();
			dictionary[PlayerMovementState.Sprinting] = 1f;
			dictionary[PlayerMovementState.Walking] = 0.6f;
			dictionary[PlayerMovementState.Sneaking] = 0.15f;
			dictionary[PlayerMovementState.Crouching] = 0.1f;
			this.StateMultipliers = dictionary;
			base..ctor();
		}
	}
}
