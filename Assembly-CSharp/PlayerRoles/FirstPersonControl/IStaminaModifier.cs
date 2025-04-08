using System;

namespace PlayerRoles.FirstPersonControl
{
	public interface IStaminaModifier
	{
		bool StaminaModifierActive { get; }

		float StaminaUsageMultiplier
		{
			get
			{
				return 1f;
			}
		}

		float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}
	}
}
