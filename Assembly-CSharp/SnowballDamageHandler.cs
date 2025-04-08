using System;
using Footprinting;
using PlayerStatsSystem;
using UnityEngine;

public class SnowballDamageHandler : AttackerDamageHandler
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
			return "Snowballed by " + this.Attacker.Nickname;
		}
	}

	public SnowballDamageHandler()
	{
		this.Attacker = default(Footprint);
		this.Damage = 0f;
		this._moveDirection = Vector3.zero;
	}

	public SnowballDamageHandler(Footprint attacker, float damage, Vector3 moveDirection)
	{
		this.Attacker = attacker;
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
		module.CurValue -= this.Damage;
		this.StartVelocity += this._moveDirection.NormalizeIgnoreY() * 15f + Vector3.up * 2f;
		if (module.CurValue > 0f)
		{
			return DamageHandlerBase.HandlerOutput.Damaged;
		}
		return DamageHandlerBase.HandlerOutput.Death;
	}

	private readonly Vector3 _moveDirection;

	private const float UpwardsForce = 2f;

	private const float HorizontalForce = 15f;
}
