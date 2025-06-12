using Footprinting;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem;

public class DisruptorDamageHandler : AttackerDamageHandler, DisintegrateDeathAnimation.IDisintegrateDamageHandler
{
	private const float ZombieDamageMultiplier = 2f;

	private const float RagdollVelocity = 15f;

	private const float VerticalVelocity = 2f;

	private const float HumePenetration = 0.5f;

	public override float Damage { get; set; }

	public override Footprint Attacker { get; protected set; }

	public override bool AllowSelfDamage => true;

	public override string ServerLogsText => "Molecularly disrupted by " + this.Attacker.Nickname;

	public override string RagdollInspectText => "Molecularly disrupted.";

	public override string DeathScreenText => string.Empty;

	public DisruptorActionModule.FiringState FiringState { get; private set; }

	public bool Disintegrate => this.FiringState == DisruptorActionModule.FiringState.FiringSingle;

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
		base.StartVelocity = flyDirection.NormalizeIgnoreY() * 15f;
		base.StartVelocity.y = 2f;
	}

	public override HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		if (ply.GetRoleId() == RoleTypeId.Scp0492)
		{
			this.Damage *= 2f;
		}
		switch (this.FiringState)
		{
		case DisruptorActionModule.FiringState.FiringRapid:
		{
			Vector3 startVelocity = base.StartVelocity;
			HandlerOutput result = base.ApplyDamage(ply);
			base.StartVelocity = startVelocity;
			return result;
		}
		case DisruptorActionModule.FiringState.FiringSingle:
		{
			float damage = this.Damage;
			this.Damage *= 0.5f;
			HandlerOutput handlerOutput = base.ApplyDamage(ply);
			if (handlerOutput == HandlerOutput.Death)
			{
				return handlerOutput;
			}
			this.Damage = damage * 0.5f;
			HealthStat module = ply.playerStats.GetModule<HealthStat>();
			this.ProcessDamage(ply);
			if (this.Damage <= 0f)
			{
				return handlerOutput;
			}
			base.DealtHealthDamage += Mathf.Min(module.CurValue, this.Damage);
			module.CurValue -= this.Damage;
			if (!(module.CurValue <= 0f))
			{
				return HandlerOutput.Damaged;
			}
			return HandlerOutput.Death;
		}
		default:
			return HandlerOutput.Nothing;
		}
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
}
