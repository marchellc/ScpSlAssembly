using System;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieAttackAbility : SingleTargetAttackAbility<ZombieRole>
	{
		public override float DamageAmount
		{
			get
			{
				return 40f;
			}
		}

		protected override float AttackDelay
		{
			get
			{
				return 0f;
			}
		}

		protected override float BaseCooldown
		{
			get
			{
				return 1.3f;
			}
		}

		protected override bool CanTriggerAbility
		{
			get
			{
				return !this._consumeAbility.IsInProgress && base.CanTriggerAbility;
			}
		}

		protected override bool SelfRepeating
		{
			get
			{
				return false;
			}
		}

		protected override DamageHandlerBase DamageHandler(float damage)
		{
			return new Scp049DamageHandler(base.Owner, damage, Scp049DamageHandler.AttackType.Scp0492);
		}

		protected override void Awake()
		{
			base.Awake();
			base.CastRole.SubroutineModule.TryGetSubroutine<ZombieConsumeAbility>(out this._consumeAbility);
		}

		private ZombieConsumeAbility _consumeAbility;
	}
}
