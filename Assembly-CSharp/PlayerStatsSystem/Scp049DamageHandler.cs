using System.Collections.Generic;
using Footprinting;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Spectating;

namespace PlayerStatsSystem;

public class Scp049DamageHandler : ScpDamageHandler
{
	public enum AttackType : byte
	{
		Instakill,
		CardiacArrest,
		Scp0492
	}

	private static readonly Dictionary<AttackType, string> LogReasons = new Dictionary<AttackType, string>
	{
		[AttackType.Instakill] = "Killed directly by SCP-049",
		[AttackType.CardiacArrest] = "Died to a heart-attack forced by SCP-049",
		[AttackType.Scp0492] = "Terminated by an instance of SCP-049-2"
	};

	private readonly string _ragdollInspectText;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => string.Empty;

	public override CassieAnnouncement CassieDeathAnnouncement => new CassieAnnouncement();

	public override Footprint Attacker { get; protected set; }

	public override string ServerLogsText => Scp049DamageHandler.LogReasons[this.DamageSubType] + " (" + this.Attacker.Nickname + ").";

	public override bool AllowSelfDamage => false;

	public AttackType DamageSubType { get; private set; }

	public Scp049DamageHandler(ReferenceHub attacker, float damage, AttackType attackType)
	{
		this.Damage = damage;
		this.DamageSubType = attackType;
		this.Attacker = new Footprint(attacker);
	}

	public Scp049DamageHandler(Footprint attacker, float damage, AttackType attackType)
	{
		this.Damage = damage;
		this.DamageSubType = attackType;
		this.Attacker = attacker;
	}

	public Scp049DamageHandler()
	{
		this._ragdollInspectText = DeathTranslations.Scp049.RagdollTranslation;
	}

	public override HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		HandlerOutput handlerOutput = base.ApplyDamage(ply);
		if (!NetworkServer.active || handlerOutput != HandlerOutput.Death)
		{
			return handlerOutput;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is Scp049Role scp049Role && scp049Role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out var subroutine))
			{
				subroutine.ServerProcessKilledPlayer(ply);
			}
		}
		return handlerOutput;
	}

	public override void WriteDeathScreen(NetworkWriter writer)
	{
		RoleTypeId role = ((this.Attacker.Role != RoleTypeId.Scp0492) ? RoleTypeId.Scp049 : RoleTypeId.Scp0492);
		writer.WriteSpawnReason(SpectatorSpawnReason.KilledByPlayer);
		writer.WriteUInt(this.Attacker.NetId);
		writer.WriteString(this.Attacker.Nickname);
		writer.WriteRoleType(role);
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte((byte)this.DamageSubType);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this.DamageSubType = (AttackType)reader.ReadByte();
	}
}
