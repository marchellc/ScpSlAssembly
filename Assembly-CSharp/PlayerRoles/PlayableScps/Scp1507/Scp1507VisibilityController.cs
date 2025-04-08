using System;
using PlayerRoles.FirstPersonControl;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507VisibilityController : FpcVisibilityController
	{
		protected override int NormalMaxRangeSqr
		{
			get
			{
				return this.SurfaceMaxRangeSqr;
			}
		}

		public override bool ValidateVisibility(ReferenceHub hub)
		{
			Scp1507Role scp1507Role = hub.roleManager.CurrentRole as Scp1507Role;
			return (scp1507Role != null && scp1507Role.Team == Team.Flamingos) || base.ValidateVisibility(hub);
		}
	}
}
