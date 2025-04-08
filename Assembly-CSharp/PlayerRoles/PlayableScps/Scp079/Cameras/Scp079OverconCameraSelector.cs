using System;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public class Scp079OverconCameraSelector : Scp079DirectionalCameraSelector
	{
		private OverconBase CurOvercon
		{
			get
			{
				return OverconManager.Singleton.HighlightedOvercon;
			}
		}

		public override bool IsVisible
		{
			get
			{
				if (!Scp079CursorManager.LockCameras)
				{
					CameraOvercon cameraOvercon = this.CurOvercon as CameraOvercon;
					if (cameraOvercon != null)
					{
						return cameraOvercon != null;
					}
				}
				return false;
			}
		}

		protected override bool AllowSwitchingBetweenZones
		{
			get
			{
				return true;
			}
		}

		protected override bool TryGetCamera(out Scp079Camera targetCamera)
		{
			if (!this.IsVisible)
			{
				targetCamera = null;
				return false;
			}
			targetCamera = (this.CurOvercon as CameraOvercon).Target;
			return true;
		}
	}
}
