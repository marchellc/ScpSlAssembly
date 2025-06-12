using Footprinting;
using InventorySystem.Items.Armor;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939DamageHandler : AttackerDamageHandler
{
	private string _ragdollInspect;

	private RagdollAnimationTemplate _lungeTemplate;

	private RelativePosition _hitPos;

	private bool _lungeTemplateSet;

	private const float LungeUpwardsSpeed = 3.5f;

	private const float LungeTotalSpeed = 5.5f;

	public override bool AllowSelfDamage => false;

	public override float Damage { get; set; }

	public override Footprint Attacker { get; protected set; }

	public override string ServerLogsText => $"Killed by SCP-939 ({this.Attacker.Nickname}) with {this.Scp939DamageType}.";

	public override string RagdollInspectText => this._ragdollInspect;

	public override string DeathScreenText => string.Empty;

	private RagdollAnimationTemplate LungeTemplate
	{
		get
		{
			if (this._lungeTemplateSet)
			{
				return this._lungeTemplate;
			}
			if (!PlayerRoleLoader.TryGetRoleTemplate<Scp939Role>(RoleTypeId.Scp939, out var result))
			{
				return null;
			}
			if (!result.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out var subroutine))
			{
				return null;
			}
			this._lungeTemplate = subroutine.LungeDeathAnim;
			this._lungeTemplateSet = true;
			return this.LungeTemplate;
		}
	}

	public Scp939DamageType Scp939DamageType { get; private set; }

	public Scp939DamageHandler(Scp939Role scp939, float damage, Scp939DamageType type = Scp939DamageType.None)
	{
		switch (type)
		{
		case Scp939DamageType.None:
			return;
		case Scp939DamageType.LungeSecondary:
			this._hitPos = new RelativePosition(scp939.FpcModule.Position);
			break;
		}
		if (scp939.TryGetOwner(out var hub))
		{
			this.Damage = damage;
			this.Attacker = new Footprint(hub);
			this.Scp939DamageType = type;
		}
	}

	protected override void ProcessDamage(ReferenceHub ply)
	{
		if (!(ply.roleManager.CurrentRole is HumanRole humanRole))
		{
			base.ProcessDamage(ply);
			return;
		}
		if (this.Scp939DamageType == Scp939DamageType.Claw)
		{
			int armorEfficacy = humanRole.GetArmorEfficacy(HitboxType.Body);
			int bulletPenetrationPercent = 75;
			this.Damage = BodyArmorUtils.ProcessDamage(armorEfficacy, this.Damage, bulletPenetrationPercent);
		}
		base.ProcessDamage(ply);
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte((byte)this.Scp939DamageType);
		if (this.Scp939DamageType == Scp939DamageType.LungeSecondary)
		{
			writer.WriteRelativePosition(this._hitPos);
		}
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this.Scp939DamageType = (Scp939DamageType)reader.ReadByte();
		if (this.Scp939DamageType == Scp939DamageType.LungeSecondary)
		{
			this._hitPos = reader.ReadRelativePosition();
		}
	}

	public override void ProcessRagdoll(BasicRagdoll ragdoll)
	{
		if (!(ragdoll is DynamicRagdoll dynamicRagdoll))
		{
			return;
		}
		switch (this.Scp939DamageType)
		{
		case Scp939DamageType.LungeTarget:
			this.LungeTemplate.ProcessRagdoll(ragdoll);
			break;
		case Scp939DamageType.LungeSecondary:
		{
			Vector3 vector = ragdoll.Info.StartPosition - this._hitPos.Position;
			vector.y = 3.5f;
			vector = vector.normalized * 5.5f;
			Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
			for (int i = 0; i < linkedRigidbodies.Length; i++)
			{
				linkedRigidbodies[i].linearVelocity = vector;
			}
			break;
		}
		default:
			base.ProcessRagdoll(ragdoll);
			break;
		}
	}
}
