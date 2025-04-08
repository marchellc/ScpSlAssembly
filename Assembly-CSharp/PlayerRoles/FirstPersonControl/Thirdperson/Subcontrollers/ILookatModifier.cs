using System;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public interface ILookatModifier
	{
		LookatData ProcessLookat(LookatData data);
	}
}
