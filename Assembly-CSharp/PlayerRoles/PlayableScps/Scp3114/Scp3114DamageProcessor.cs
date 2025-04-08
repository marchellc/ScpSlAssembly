using System;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114DamageProcessor : SubroutineBase, IDamageHandlerProcessingRole
	{
		private void DisableHitboxMultipliers(DamageHandlerBase handler)
		{
			StandardDamageHandler standardDamageHandler = handler as StandardDamageHandler;
			if (standardDamageHandler == null)
			{
				return;
			}
			standardDamageHandler.Hitbox = HitboxType.Body;
		}

		private void ApplyExplosionDamageReduction(ExplosionDamageHandler explosionHandler)
		{
			explosionHandler.Damage *= this._explosionDamageMultiplier;
		}

		private void ApplyDisguiseDamageReduction(FirearmDamageHandler firearmHandler)
		{
			float num = ((this._scpRole.HumeShieldModule.HsCurrent > 0f) ? this._disguisedHumeShieldDamageMultiplier : this._disguisedBaseHealthDamageMultiplier);
			firearmHandler.Damage *= num;
		}

		protected override void Awake()
		{
			base.Awake();
			this._scpRole = base.Role as Scp3114Role;
		}

		public DamageHandlerBase ProcessDamageHandler(DamageHandlerBase handler)
		{
			this.DisableHitboxMultipliers(handler);
			ExplosionDamageHandler explosionDamageHandler = handler as ExplosionDamageHandler;
			if (explosionDamageHandler == null)
			{
				FirearmDamageHandler firearmDamageHandler = handler as FirearmDamageHandler;
				if (firearmDamageHandler != null)
				{
					if (this._scpRole.Disguised)
					{
						this.ApplyDisguiseDamageReduction(firearmDamageHandler);
					}
				}
			}
			else
			{
				this.ApplyExplosionDamageReduction(explosionDamageHandler);
			}
			return handler;
		}

		[SerializeField]
		private float _explosionDamageMultiplier;

		[SerializeField]
		private float _disguisedHumeShieldDamageMultiplier;

		[SerializeField]
		private float _disguisedBaseHealthDamageMultiplier;

		private Scp3114Role _scpRole;
	}
}
