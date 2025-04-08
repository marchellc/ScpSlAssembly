using System;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class PitDeath : StatusEffectBase, IDamageModifierEffect
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public static bool ValidatePlayer(ReferenceHub hub)
		{
			PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
			IFpcRole fpcRole = currentRole as IFpcRole;
			return fpcRole != null && !fpcRole.FpcModule.Noclip.IsActive && !hub.characterClassManager.GodMode && currentRole.ActiveTime >= 1f;
		}

		public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
		{
			return (float)(this.IsFallDamageHandler(handler) ? 0 : 1);
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._activeElapsed = 0f;
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			if (!NetworkServer.active)
			{
				return;
			}
			this._activeElapsed += Time.deltaTime;
			this.CheckKillConditions();
		}

		private bool IsFallDamageHandler(DamageHandlerBase handler)
		{
			UniversalDamageHandler universalDamageHandler = handler as UniversalDamageHandler;
			return universalDamageHandler != null && universalDamageHandler.TranslationId == DeathTranslations.Falldown.Id;
		}

		private void CheckKillConditions()
		{
			if (!PitDeath.ValidatePlayer(base.Hub))
			{
				this.DisableEffect();
				return;
			}
			if (this._activeElapsed <= 1.2f && !base.Hub.IsGrounded())
			{
				return;
			}
			this.KillPlayer();
		}

		private void KillPlayer()
		{
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Crushed, null));
		}

		private const float MinAliveDuration = 1f;

		private const float FallbackMaxDelay = 1.2f;

		private float _activeElapsed;
	}
}
