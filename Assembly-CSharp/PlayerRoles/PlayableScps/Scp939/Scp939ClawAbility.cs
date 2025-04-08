using System;
using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939ClawAbility : ScpAttackAbilityBase<Scp939Role>
	{
		public override float DamageAmount
		{
			get
			{
				return 40f;
			}
		}

		protected override float BaseCooldown
		{
			get
			{
				return 0.8f;
			}
		}

		protected override bool CanTriggerAbility
		{
			get
			{
				return base.CanTriggerAbility && this._focusAbility.State == 0f && !this._cloudAbility.TargetState;
			}
		}

		protected override DamageHandlerBase DamageHandler(float damage)
		{
			return new Scp939DamageHandler(base.CastRole, damage, Scp939DamageType.Claw);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			if (this._focusAbility.State != 0f)
			{
				return;
			}
			base.ServerProcessCmd(reader);
		}

		protected override void DamagePlayers()
		{
			int num = Mathf.Max(0, this.DetectedPlayers.Count - 1);
			ReferenceHub primaryTarget = this.DetectedPlayers.GetPrimaryTarget(base.Owner.PlayerCameraReference);
			foreach (ReferenceHub referenceHub in this.DetectedPlayers)
			{
				if (referenceHub == primaryTarget)
				{
					this.DamagePlayer(referenceHub, this.DamageAmount);
				}
				else
				{
					this.DamagePlayer(referenceHub, this.DamageAmount / (float)num);
				}
			}
		}

		protected override void DamagePlayer(ReferenceHub hub, float damage)
		{
			Scp939AttackingEventArgs scp939AttackingEventArgs = new Scp939AttackingEventArgs(base.Owner, hub, damage);
			Scp939Events.OnAttacking(scp939AttackingEventArgs);
			if (!scp939AttackingEventArgs.IsAllowed)
			{
				return;
			}
			hub = scp939AttackingEventArgs.Target.ReferenceHub;
			damage = scp939AttackingEventArgs.Damage;
			base.DamagePlayer(hub, damage);
			Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, hub, damage));
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp939FocusAbility>(out this._focusAbility);
			base.GetSubroutine<Scp939AmnesticCloudAbility>(out this._cloudAbility);
		}

		public const float BaseDamage = 40f;

		public const int DamagePenetration = 75;

		private Scp939FocusAbility _focusAbility;

		private Scp939AmnesticCloudAbility _cloudAbility;
	}
}
