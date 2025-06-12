using PlayerRoles.FirstPersonControl;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507VisibilityController : FpcVisibilityController
{
	protected override int NormalMaxRangeSqr => this.SurfaceMaxRangeSqr;

	public override bool ValidateVisibility(ReferenceHub hub)
	{
		if (hub.roleManager.CurrentRole is Scp1507Role { Team: Team.Flamingos })
		{
			return true;
		}
		return base.ValidateVisibility(hub);
	}
}
