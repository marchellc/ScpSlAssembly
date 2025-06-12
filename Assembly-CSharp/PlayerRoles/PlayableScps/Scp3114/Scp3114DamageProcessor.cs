using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114DamageProcessor : SubroutineBase, IDamageHandlerProcessingRole
{
	[SerializeField]
	private float _explosionDamageMultiplier;

	[SerializeField]
	private float _disguisedHumeShieldDamageMultiplier;

	[SerializeField]
	private float _disguisedBaseHealthDamageMultiplier;

	private Scp3114Role _scpRole;

	private void DisableHitboxMultipliers(DamageHandlerBase handler)
	{
		if (handler is StandardDamageHandler standardDamageHandler)
		{
			standardDamageHandler.Hitbox = HitboxType.Body;
		}
	}

	private void ApplyExplosionDamageReduction(ExplosionDamageHandler explosionHandler)
	{
		explosionHandler.Damage *= this._explosionDamageMultiplier;
	}

	private void ApplyDisguiseDamageReduction(FirearmDamageHandler firearmHandler)
	{
		float num = ((this._scpRole.HumeShieldModule.HsCurrent > 0f) ? this._disguisedHumeShieldDamageMultiplier : this._disguisedBaseHealthDamageMultiplier);
		firearmHandler.Damage *= num;
	}

	protected override void Awake()
	{
		base.Awake();
		this._scpRole = base.Role as Scp3114Role;
	}

	public DamageHandlerBase ProcessDamageHandler(DamageHandlerBase handler)
	{
		this.DisableHitboxMultipliers(handler);
		if (!(handler is ExplosionDamageHandler explosionHandler))
		{
			if (handler is FirearmDamageHandler firearmHandler && this._scpRole.Disguised)
			{
				this.ApplyDisguiseDamageReduction(firearmHandler);
			}
		}
		else
		{
			this.ApplyExplosionDamageReduction(explosionHandler);
		}
		return handler;
	}
}
