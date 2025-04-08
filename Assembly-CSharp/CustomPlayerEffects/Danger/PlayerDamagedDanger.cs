using System;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects.Danger
{
	public class PlayerDamagedDanger : ParentDangerBase
	{
		public override void Initialize(ReferenceHub target)
		{
			base.Initialize(target);
			this._leftoverDamage = 0f;
			PlayerStats.OnAnyPlayerDamaged += this.UpdateState;
		}

		public override void Dispose()
		{
			base.Dispose();
			PlayerStats.OnAnyPlayerDamaged -= this.UpdateState;
		}

		private void UpdateState(ReferenceHub damagedHub, DamageHandlerBase damageHandler)
		{
			if (damagedHub != base.Owner)
			{
				return;
			}
			if (damageHandler is Scp049DamageHandler)
			{
				return;
			}
			AttackerDamageHandler attackerDamageHandler = damageHandler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				UniversalDamageHandler universalDamageHandler = damageHandler as UniversalDamageHandler;
				if (universalDamageHandler != null)
				{
					if (universalDamageHandler.TranslationId == DeathTranslations.PocketDecay.Id)
					{
						return;
					}
				}
			}
			else if (attackerDamageHandler.Attacker.Hub.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp106)
			{
				return;
			}
			StandardDamageHandler standardDamageHandler = damageHandler as StandardDamageHandler;
			if (standardDamageHandler == null)
			{
				return;
			}
			float num = Mathf.Floor((standardDamageHandler.DealtHealthDamage + this._leftoverDamage) / 10f) * 0.25f;
			if (standardDamageHandler.DealtHealthDamage % 10f + this._leftoverDamage >= 10f)
			{
				this._leftoverDamage = 0f;
			}
			this._leftoverDamage += standardDamageHandler.DealtHealthDamage % 10f;
			if (num < 0.25f)
			{
				return;
			}
			base.ChildDangers.Add(new ExpiringDanger(num, base.Owner));
		}

		private const int DangerStepThreshold = 10;

		private const float DangerPerThreshold = 0.25f;

		private float _leftoverDamage;
	}
}
