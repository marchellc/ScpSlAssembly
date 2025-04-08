using System;

namespace PlayerRoles.PlayableScps.HUDs
{
	public abstract class ViewmodelScpHud : ScpHudBase, IViewmodelRole
	{
		public ScpViewmodelBase Viewmodel { get; protected set; }

		public bool TryGetViewmodelFov(out float fov)
		{
			fov = this.Viewmodel.CamFOV;
			return true;
		}
	}
}
