using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114MovementModule : FirstPersonMovementModule, IStaminaModifier
	{
		protected override FpcStateProcessor NewStateProcessor
		{
			get
			{
				return new SubroutineInfluencedFpcStateProcessor(base.Hub, this, FpcStateProcessor.DefaultUseRate, FpcStateProcessor.DefaultSpawnImmunity, FpcStateProcessor.DefaultRegenCooldown, FpcStateProcessor.DefaultRegenSpeed, 3.11f);
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return this._scpRole.SkeletonIdle;
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return 4f;
			}
		}

		private void Awake()
		{
			this._scpRole = base.GetComponent<Scp3114Role>();
			this._skeletonWalkSpeed = this.WalkSpeed;
			this._skeletonSprintSpeed = this.SprintSpeed;
			this._scpRole.CurIdentity.OnStatusChanged += this.OnStatusChanged;
		}

		private void OnStatusChanged()
		{
			Scp3114Identity.DisguiseStatus status = this._scpRole.CurIdentity.Status;
			if (status != Scp3114Identity.DisguiseStatus.None)
			{
				HumanRole humanRole;
				if (status == Scp3114Identity.DisguiseStatus.Active && PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this._scpRole.CurIdentity.StolenRole, out humanRole))
				{
					this.WalkSpeed = humanRole.FpcModule.WalkSpeed;
					this.SprintSpeed = humanRole.FpcModule.SprintSpeed;
					return;
				}
			}
			else
			{
				this.WalkSpeed = this._skeletonWalkSpeed;
				this.SprintSpeed = this._skeletonSprintSpeed;
			}
		}

		private Scp3114Role _scpRole;

		private float _skeletonWalkSpeed;

		private float _skeletonSprintSpeed;
	}
}
