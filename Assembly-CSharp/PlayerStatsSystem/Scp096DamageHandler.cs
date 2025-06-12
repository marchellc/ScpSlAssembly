using System.Collections.Generic;
using Footprinting;
using PlayerRoles.PlayableScps.Scp096;
using UnityEngine;

namespace PlayerStatsSystem;

public class Scp096DamageHandler : ScpDamageHandler
{
	public enum AttackType
	{
		GateKill,
		SlapLeft,
		SlapRight,
		Charge
	}

	private static readonly Dictionary<AttackType, string> LogReasons = new Dictionary<AttackType, string>
	{
		[AttackType.SlapLeft] = "Got slapped by SCP-096's left hand",
		[AttackType.SlapRight] = "Got slapped by SCP-096's right hand",
		[AttackType.Charge] = "Stood in a line of SCP-096's charge",
		[AttackType.GateKill] = "Tried to pass through a gate being breached by SCP-096"
	};

	private readonly string _ragdollInspectText;

	private readonly AttackType _attackType;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => string.Empty;

	public override CassieAnnouncement CassieDeathAnnouncement => new CassieAnnouncement();

	public override Footprint Attacker { get; protected set; }

	public override string ServerLogsText => Scp096DamageHandler.LogReasons[this._attackType] + " (" + this.Attacker.Nickname + ").";

	public override bool AllowSelfDamage => false;

	public Scp096DamageHandler(Scp096Role attacker, float damage, AttackType attackType)
	{
		this.Damage = damage;
		if (!(attacker == null) && attacker.TryGetOwner(out var hub))
		{
			this._attackType = attackType;
			this.Attacker = new Footprint(hub);
		}
	}

	public override HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		HandlerOutput result = base.ApplyDamage(ply);
		Vector3 normalized = (ply.transform.position - this.Attacker.Hub.transform.position).normalized;
		switch (this._attackType)
		{
		case AttackType.Charge:
			base.StartVelocity = normalized * 8f;
			base.StartVelocity.y = 3.5f;
			break;
		case AttackType.SlapLeft:
		case AttackType.SlapRight:
		{
			Vector3 right = this.Attacker.Hub.PlayerCameraReference.right;
			if (this._attackType == AttackType.SlapLeft)
			{
				right *= -1f;
			}
			right += this.Attacker.Hub.transform.forward;
			right += Vector3.up;
			base.StartVelocity = right * (Random.value + 1.5f) * 3f;
			break;
		}
		case AttackType.GateKill:
			base.StartVelocity = normalized * 2f;
			base.StartVelocity.y = -10f;
			break;
		}
		return result;
	}

	public Scp096DamageHandler()
	{
		this._ragdollInspectText = DeathTranslations.Scp096.RagdollTranslation;
	}
}
