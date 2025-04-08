using System;
using Footprinting;
using InventorySystem.Items.Armor;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939DamageHandler : AttackerDamageHandler
	{
		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override string ServerLogsText
		{
			get
			{
				return string.Format("Killed by SCP-939 ({0}) with {1}.", this.Attacker.Nickname, this._damageType);
			}
		}

		private RagdollAnimationTemplate LungeTemplate
		{
			get
			{
				if (this._lungeTemplateSet)
				{
					return this._lungeTemplate;
				}
				Scp939Role scp939Role;
				if (!PlayerRoleLoader.TryGetRoleTemplate<Scp939Role>(RoleTypeId.Scp939, out scp939Role))
				{
					return null;
				}
				Scp939LungeAbility scp939LungeAbility;
				if (!scp939Role.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out scp939LungeAbility))
				{
					return null;
				}
				this._lungeTemplate = scp939LungeAbility.LungeDeathAnim;
				this._lungeTemplateSet = true;
				return this.LungeTemplate;
			}
		}

		public Scp939DamageType Scp939DamageType
		{
			get
			{
				return this._damageType;
			}
		}

		public Scp939DamageHandler(Scp939Role scp939, float damage, Scp939DamageType type = Scp939DamageType.None)
		{
			if (type == Scp939DamageType.None)
			{
				return;
			}
			if (type == Scp939DamageType.LungeSecondary)
			{
				this._hitPos = new RelativePosition(scp939.FpcModule.Position);
			}
			ReferenceHub referenceHub;
			if (!scp939.TryGetOwner(out referenceHub))
			{
				return;
			}
			this.Damage = damage;
			this.Attacker = new Footprint(referenceHub);
			this._damageType = type;
		}

		protected override void ProcessDamage(ReferenceHub ply)
		{
			HumanRole humanRole = ply.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				base.ProcessDamage(ply);
				return;
			}
			if (this._damageType == Scp939DamageType.Claw)
			{
				int armorEfficacy = humanRole.GetArmorEfficacy(HitboxType.Body);
				int num = 75;
				this.Damage = BodyArmorUtils.ProcessDamage(armorEfficacy, this.Damage, num);
			}
			base.ProcessDamage(ply);
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte((byte)this._damageType);
			if (this._damageType == Scp939DamageType.LungeSecondary)
			{
				writer.WriteRelativePosition(this._hitPos);
			}
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this._damageType = (Scp939DamageType)reader.ReadByte();
			if (this._damageType == Scp939DamageType.LungeSecondary)
			{
				this._hitPos = reader.ReadRelativePosition();
			}
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			DynamicRagdoll dynamicRagdoll = ragdoll as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				return;
			}
			Scp939DamageType damageType = this._damageType;
			if (damageType == Scp939DamageType.LungeTarget)
			{
				this.LungeTemplate.ProcessRagdoll(ragdoll);
				return;
			}
			if (damageType != Scp939DamageType.LungeSecondary)
			{
				base.ProcessRagdoll(ragdoll);
				return;
			}
			Vector3 vector = ragdoll.Info.StartPosition - this._hitPos.Position;
			vector.y = 3.5f;
			vector = vector.normalized * 5.5f;
			Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
			for (int i = 0; i < linkedRigidbodies.Length; i++)
			{
				linkedRigidbodies[i].velocity = vector;
			}
		}

		private Scp939DamageType _damageType;

		private RagdollAnimationTemplate _lungeTemplate;

		private RelativePosition _hitPos;

		private bool _lungeTemplateSet;

		private const float LungeUpwardsSpeed = 3.5f;

		private const float LungeTotalSpeed = 5.5f;
	}
}
