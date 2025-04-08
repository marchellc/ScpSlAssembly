using System;
using PlayerRoles.FirstPersonControl;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieVisibilityController : FpcVisibilityController
	{
		public override bool ValidateVisibility(ReferenceHub hub)
		{
			return base.ValidateVisibility(hub) || hub.roleManager.CurrentRole is Scp049Role;
		}
	}
}
