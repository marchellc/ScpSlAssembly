using System;
using PlayerRoles.FirstPersonControl;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939MovementModule : FirstPersonMovementModule
	{
		protected override FpcMotor NewMotor
		{
			get
			{
				return new Scp939Motor(base.Hub, base.Role as Scp939Role);
			}
		}

		protected override FpcMouseLook NewMouseLook
		{
			get
			{
				return new Scp939MouseLook(base.Hub, this);
			}
		}

		protected override FpcStateProcessor NewStateProcessor
		{
			get
			{
				return new Scp939StateProcessor(base.Hub, this, 0.09f, 7f, 0.1f, 0.1f);
			}
		}

		protected override PlayerMovementState ValidateMovementState(PlayerMovementState state)
		{
			state = base.ValidateMovementState(state);
			Scp939Motor scp939Motor = base.Motor as Scp939Motor;
			if (state == PlayerMovementState.Sprinting && !scp939Motor.MovingForwards)
			{
				state = PlayerMovementState.Walking;
			}
			return state;
		}

		private const float StaminaUseRate = 0.09f;

		private const float StaminaRegenRate = 0.1f;

		private const float StaminaRegenCooldown = 7f;

		private const float StaminaRampupTime = 0.1f;
	}
}
