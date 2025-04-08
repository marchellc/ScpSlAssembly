using System;
using Footprinting;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class JailbirdDamageHandler : AttackerDamageHandler
	{
		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return "Jailbirded by " + this.Attacker.Nickname;
			}
		}

		public JailbirdDamageHandler()
		{
			this.Attacker = default(Footprint);
			this.Damage = 0f;
			this._moveDirection = Vector3.zero;
		}

		public JailbirdDamageHandler(ReferenceHub attacker, float damage, Vector3 moveDirection)
		{
			this.Attacker = new Footprint(attacker);
			this.Damage = damage;
			this._moveDirection = moveDirection;
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			HealthStat module = ply.playerStats.GetModule<HealthStat>();
			this.ProcessDamage(ply);
			if (this.Damage <= 0f)
			{
				return DamageHandlerBase.HandlerOutput.Nothing;
			}
			if (ply.GetRoleId() == RoleTypeId.Scp0492)
			{
				this.Damage *= 4f;
			}
			module.CurValue -= this.Damage;
			this.StartVelocity += (this._moveDirection.NormalizeIgnoreY() * 0.1f + Vector3.up * 0.02f) * this.Damage;
			if (module.CurValue > 0f)
			{
				return DamageHandlerBase.HandlerOutput.Damaged;
			}
			return DamageHandlerBase.HandlerOutput.Death;
		}

		private readonly Vector3 _moveDirection;

		private const float ZombieDamageMultiplier = 4f;

		private const float UpwardsForce = 0.02f;

		private const float HorizontalForce = 0.1f;
	}
}
