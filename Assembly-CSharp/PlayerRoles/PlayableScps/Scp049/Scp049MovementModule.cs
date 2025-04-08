using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049MovementModule : FirstPersonMovementModule
	{
		private float MovementSpeed
		{
			set
			{
				this.WalkSpeed = value;
				this.SprintSpeed = value;
			}
		}

		private void Awake()
		{
			this._normalSpeed = this.WalkSpeed;
			this._enragedSpeed = this.SprintSpeed;
			this._role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out this._senseAbility);
		}

		protected override void UpdateMovement()
		{
			this.MovementSpeed = (this._senseAbility.HasTarget ? this._enragedSpeed : this._normalSpeed);
			base.UpdateMovement();
		}

		[SerializeField]
		private Scp049Role _role;

		private float _normalSpeed;

		private float _enragedSpeed;

		private Scp049SenseAbility _senseAbility;
	}
}
