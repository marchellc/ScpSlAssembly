using System;
using Footprinting;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class DisruptorDamageHandler : AttackerDamageHandler, DisintegrateDeathAnimation.IDisintegrateDamageHandler
	{
		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return true;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return "Molecularly disrupted by " + this.Attacker.Nickname;
			}
		}

		public DisruptorActionModule.FiringState FiringState { get; private set; }

		public bool Disintegrate
		{
			get
			{
				return this.FiringState == DisruptorActionModule.FiringState.FiringSingle;
			}
		}

		public DisruptorDamageHandler(DisruptorShotEvent shotEvent, Vector3 flyDirection, float damage)
		{
			this.Damage = damage;
			if (shotEvent == null)
			{
				this.Attacker = default(Footprint);
				this.FiringState = DisruptorActionModule.FiringState.None;
			}
			else
			{
				this.Attacker = shotEvent.HitregFootprint;
				this.FiringState = shotEvent.State;
			}
			this.StartVelocity = flyDirection.NormalizeIgnoreY() * 15f;
			this.StartVelocity.y = 2f;
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			if (ply.GetRoleId() == RoleTypeId.Scp0492)
			{
				this.Damage *= 2f;
			}
			DisruptorActionModule.FiringState firingState = this.FiringState;
			if (firingState == DisruptorActionModule.FiringState.FiringRapid)
			{
				Vector3 startVelocity = this.StartVelocity;
				DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
				this.StartVelocity = startVelocity;
				return handlerOutput;
			}
			if (firingState != DisruptorActionModule.FiringState.FiringSingle)
			{
				return DamageHandlerBase.HandlerOutput.Nothing;
			}
			float damage = this.Damage;
			this.Damage *= 0.5f;
			DamageHandlerBase.HandlerOutput handlerOutput2 = base.ApplyDamage(ply);
			if (handlerOutput2 == DamageHandlerBase.HandlerOutput.Death)
			{
				return handlerOutput2;
			}
			this.Damage = damage * 0.5f;
			HealthStat module = ply.playerStats.GetModule<HealthStat>();
			this.ProcessDamage(ply);
			if (this.Damage <= 0f)
			{
				return handlerOutput2;
			}
			base.DealtHealthDamage += Mathf.Min(module.CurValue, this.Damage);
			module.CurValue -= this.Damage;
			if (module.CurValue > 0f)
			{
				return DamageHandlerBase.HandlerOutput.Damaged;
			}
			return DamageHandlerBase.HandlerOutput.Death;
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte((byte)this.FiringState);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this.FiringState = (DisruptorActionModule.FiringState)reader.ReadByte();
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			if (this.FiringState == DisruptorActionModule.FiringState.FiringRapid)
			{
				base.ProcessRagdoll(ragdoll);
			}
		}

		private const float ZombieDamageMultiplier = 2f;

		private const float RagdollVelocity = 15f;

		private const float VerticalVelocity = 2f;

		private const float HumePenetration = 0.5f;
	}
}
