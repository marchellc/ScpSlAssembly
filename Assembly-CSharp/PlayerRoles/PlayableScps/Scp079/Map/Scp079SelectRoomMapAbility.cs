using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079SelectRoomMapAbility : Scp079DirectionalCameraSelector
	{
		public override bool IsVisible
		{
			get
			{
				return Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen && Scp079MapGui.HighlightedCamera != null;
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
			targetCamera = Scp079MapGui.HighlightedCamera;
			return true;
		}

		protected override void Trigger()
		{
			base.Trigger();
			Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen = false;
		}
	}
}
