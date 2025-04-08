using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507MovementModule : FirstPersonMovementModule
	{
		private float SpeedBoost
		{
			get
			{
				return Mathf.Min((float)this._swarmAbility.FlockSize * 0.2f, 1.4f);
			}
		}

		private float MovementSpeed
		{
			get
			{
				return this.WalkSpeed;
			}
			set
			{
				this.WalkSpeed = value;
				this.SprintSpeed = value;
				this.CrouchSpeed = value;
			}
		}

		protected override void UpdateMovement()
		{
			base.UpdateMovement();
			this.MovementSpeed = this._cachedDefaultSpeed + this.SpeedBoost;
		}

		private void Awake()
		{
			this._scp1507.SubroutineModule.TryGetSubroutine<Scp1507SwarmAbility>(out this._swarmAbility);
			this._cachedDefaultSpeed = this.MovementSpeed;
		}

		private const float SpeedPerSwarmAlly = 0.2f;

		[SerializeField]
		private Scp1507Role _scp1507;

		private Scp1507SwarmAbility _swarmAbility;

		private float _cachedDefaultSpeed;
	}
}
