using System;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Slap : ScpAttackAbilityBase<Scp3114Role>
{
	private const float HitHumeShieldReward = 25f;

	[SerializeField]
	private AudioClip[] _swingClips;

	[SerializeField]
	private float _swingSoundRange;

	private HumeShieldStat _humeShield;

	private Scp3114Strangle _strangle;

	public override float DamageAmount => 15f;

	protected override float AttackDelay => 0.3f;

	protected override float BaseCooldown => 0.5f;

	protected override bool CanTriggerAbility
	{
		get
		{
			if (base.CanTriggerAbility)
			{
				return !this.AbilityBlocked;
			}
			return false;
		}
	}

	private bool AbilityBlocked
	{
		get
		{
			if (!this._strangle.SyncTarget.HasValue)
			{
				return base.CastRole.CurIdentity.Status != Scp3114Identity.DisguiseStatus.None;
			}
			return true;
		}
	}

	public event Action ServerOnHit;

	public event Action ServerOnKill;

	private void PlaySwingSound()
	{
		AudioSourcePoolManager.PlayOnTransform(this._swingClips.RandomItem(), base.transform, this._swingSoundRange, 1f, FalloffType.Exponential, MixerChannel.NoDucking);
	}

	protected override DamageHandlerBase DamageHandler(float damage)
	{
		return new Scp3114DamageHandler(base.Owner, damage, Scp3114DamageHandler.HandlerType.Slap);
	}

	protected override void Awake()
	{
		base.Awake();
		base.OnTriggered += PlaySwingSound;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		base.GetSubroutine<Scp3114Strangle>(out this._strangle);
		this._humeShield = base.Owner.playerStats.GetModule<HumeShieldStat>();
	}

	protected override void DamagePlayers()
	{
		Transform playerCameraReference = base.Owner.PlayerCameraReference;
		ReferenceHub primaryTarget = base.DetectedPlayers.GetPrimaryTarget(playerCameraReference);
		if (!(primaryTarget == null))
		{
			this.DamagePlayer(primaryTarget, this.DamageAmount);
			if (base.HasAttackResultFlag(AttackResult.KilledPlayer))
			{
				this.ServerOnKill?.Invoke();
			}
			if (base.HasAttackResultFlag(AttackResult.AttackedPlayer))
			{
				this.ServerOnHit?.Invoke();
			}
			float a = this._humeShield.CurValue + 25f;
			this._humeShield.CurValue = Mathf.Min(a, this._humeShield.MaxValue);
		}
	}
}
