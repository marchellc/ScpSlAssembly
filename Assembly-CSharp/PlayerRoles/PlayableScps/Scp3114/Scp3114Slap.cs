using System;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Slap : ScpAttackAbilityBase<Scp3114Role>
	{
		public override float DamageAmount
		{
			get
			{
				return 15f;
			}
		}

		protected override float AttackDelay
		{
			get
			{
				return 0.3f;
			}
		}

		protected override float BaseCooldown
		{
			get
			{
				return 0.5f;
			}
		}

		protected override bool CanTriggerAbility
		{
			get
			{
				return base.CanTriggerAbility && !this.AbilityBlocked;
			}
		}

		private bool AbilityBlocked
		{
			get
			{
				return this._strangle.SyncTarget != null || base.CastRole.CurIdentity.Status > Scp3114Identity.DisguiseStatus.None;
			}
		}

		public event Action ServerOnHit;

		public event Action ServerOnKill;

		private void PlaySwingSound()
		{
			AudioSourcePoolManager.PlayOnTransform(this._swingClips.RandomItem<AudioClip>(), base.transform, this._swingSoundRange, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f);
		}

		protected override DamageHandlerBase DamageHandler(float damage)
		{
			return new Scp3114DamageHandler(base.Owner, damage, Scp3114DamageHandler.HandlerType.Slap);
		}

		protected override void Awake()
		{
			base.Awake();
			base.OnTriggered += this.PlaySwingSound;
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
			ReferenceHub primaryTarget = this.DetectedPlayers.GetPrimaryTarget(playerCameraReference);
			if (primaryTarget == null)
			{
				return;
			}
			this.DamagePlayer(primaryTarget, this.DamageAmount);
			if (base.HasAttackResultFlag(AttackResult.KilledPlayer))
			{
				Action serverOnKill = this.ServerOnKill;
				if (serverOnKill != null)
				{
					serverOnKill();
				}
			}
			if (base.HasAttackResultFlag(AttackResult.AttackedPlayer))
			{
				Action serverOnHit = this.ServerOnHit;
				if (serverOnHit != null)
				{
					serverOnHit();
				}
			}
			float num = this._humeShield.CurValue + 25f;
			this._humeShield.CurValue = Mathf.Min(num, this._humeShield.MaxValue);
		}

		private const float HitHumeShieldReward = 25f;

		[SerializeField]
		private AudioClip[] _swingClips;

		[SerializeField]
		private float _swingSoundRange;

		private HumeShieldStat _humeShield;

		private Scp3114Strangle _strangle;
	}
}
