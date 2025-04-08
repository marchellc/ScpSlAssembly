using System;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem
{
	public abstract class StandardDamageHandler : DamageHandlerBase
	{
		public abstract float Damage { get; internal set; }

		public float DealtHealthDamage { get; protected set; }

		public float AbsorbedAhpDamage { get; protected set; }

		public float AbsorbedHumeDamage { get; protected set; }

		public override string ServerMetricsText
		{
			get
			{
				return string.Empty;
			}
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			this.StartVelocity = ply.GetVelocity();
			this.StartVelocity.y = Mathf.Max(this.StartVelocity.y, 0f);
			PlayerStats playerStats = ply.playerStats;
			IHealthbarRole healthbarRole = ply.roleManager.CurrentRole as IHealthbarRole;
			if (healthbarRole != null && healthbarRole.TargetStats != playerStats)
			{
				return DamageHandlerBase.HandlerOutput.Nothing;
			}
			AhpStat module = playerStats.GetModule<AhpStat>();
			HealthStat module2 = playerStats.GetModule<HealthStat>();
			HumeShieldStat module3 = playerStats.GetModule<HumeShieldStat>();
			if (this.Damage == -1f)
			{
				module.CurValue = 0f;
				module2.CurValue = 0f;
				return DamageHandlerBase.HandlerOutput.Death;
			}
			this.ProcessDamage(ply);
			StatusEffectBase[] allEffects = ply.playerEffectsController.AllEffects;
			for (int i = 0; i < allEffects.Length; i++)
			{
				IDamageModifierEffect damageModifierEffect = allEffects[i] as IDamageModifierEffect;
				if (damageModifierEffect != null && damageModifierEffect.DamageModifierActive)
				{
					this.Damage *= damageModifierEffect.GetDamageModifier(this.Damage, this, this.Hitbox);
				}
			}
			if (this.Damage <= 0f)
			{
				return DamageHandlerBase.HandlerOutput.Nothing;
			}
			float curValue = module2.CurValue;
			float num = module.ServerProcessDamage(this.Damage);
			this.AbsorbedAhpDamage = this.Damage - num;
			this.AbsorbedHumeDamage = Mathf.Min(module3.CurValue, num);
			float num2 = module3.CurValue - num;
			if (num2 < 0f)
			{
				module2.CurValue += num2;
			}
			module3.CurValue = num2;
			this.DealtHealthDamage = curValue - module2.CurValue;
			if (module2.CurValue > 0f)
			{
				return DamageHandlerBase.HandlerOutput.Damaged;
			}
			return DamageHandlerBase.HandlerOutput.Death;
		}

		protected virtual void ProcessDamage(ReferenceHub ply)
		{
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			this._velX = (short)Mathf.RoundToInt(this.StartVelocity.x * 100f);
			this._velY = (short)Mathf.RoundToInt(this.StartVelocity.y * 100f);
			this._velZ = (short)Mathf.RoundToInt(this.StartVelocity.z * 100f);
			writer.WriteShort(this._velX);
			writer.WriteShort(this._velY);
			writer.WriteShort(this._velZ);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this._velX = reader.ReadShort();
			this._velY = reader.ReadShort();
			this._velZ = reader.ReadShort();
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			base.ProcessRagdoll(ragdoll);
			DynamicRagdoll dynamicRagdoll = ragdoll as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				return;
			}
			Vector3 vector = new Vector3((float)this._velX, (float)this._velY, (float)this._velZ) / 100f;
			Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
			for (int i = 0; i < linkedRigidbodies.Length; i++)
			{
				linkedRigidbodies[i].velocity = vector;
			}
		}

		public const float KillValue = -1f;

		public HitboxType Hitbox;

		protected Vector3 StartVelocity;

		private short _velX;

		private short _velY;

		private short _velZ;
	}
}
