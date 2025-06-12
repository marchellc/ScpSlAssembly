using CustomPlayerEffects;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem;

public abstract class StandardDamageHandler : DamageHandlerBase
{
	public const float KillValue = -1f;

	public HitboxType Hitbox;

	protected Vector3 StartVelocity;

	private short _velX;

	private short _velY;

	private short _velZ;

	public abstract float Damage { get; set; }

	public float DealtHealthDamage { get; protected set; }

	public float AbsorbedAhpDamage { get; protected set; }

	public float AbsorbedHumeDamage { get; protected set; }

	public float TotalDamageDealt => this.DealtHealthDamage + this.AbsorbedAhpDamage + this.AbsorbedHumeDamage;

	public override string ServerMetricsText => string.Empty;

	public override HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		this.StartVelocity = ply.GetVelocity();
		this.StartVelocity.y = Mathf.Max(this.StartVelocity.y, 0f);
		PlayerStats playerStats = ply.playerStats;
		if (ply.roleManager.CurrentRole is IHealthbarRole healthbarRole && healthbarRole.TargetStats != playerStats)
		{
			return HandlerOutput.Nothing;
		}
		AhpStat module = playerStats.GetModule<AhpStat>();
		HealthStat module2 = playerStats.GetModule<HealthStat>();
		HumeShieldStat module3 = playerStats.GetModule<HumeShieldStat>();
		if (this.Damage == -1f)
		{
			module.CurValue = 0f;
			module2.CurValue = 0f;
			return HandlerOutput.Death;
		}
		this.ProcessDamage(ply);
		if (this.Damage <= 0f)
		{
			return HandlerOutput.Nothing;
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
		if (!(module2.CurValue <= 0f))
		{
			return HandlerOutput.Damaged;
		}
		return HandlerOutput.Death;
	}

	protected virtual void ProcessDamage(ReferenceHub ply)
	{
		StatusEffectBase[] allEffects = ply.playerEffectsController.AllEffects;
		for (int i = 0; i < allEffects.Length; i++)
		{
			if (allEffects[i] is IDamageModifierEffect { DamageModifierActive: not false } damageModifierEffect)
			{
				this.Damage *= damageModifierEffect.GetDamageModifier(this.Damage, this, this.Hitbox);
			}
		}
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
		if (ragdoll is DynamicRagdoll dynamicRagdoll)
		{
			Vector3 linearVelocity = new Vector3(this._velX, this._velY, this._velZ) / 100f;
			Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
			for (int i = 0; i < linkedRigidbodies.Length; i++)
			{
				linkedRigidbodies[i].linearVelocity = linearVelocity;
			}
		}
	}
}
