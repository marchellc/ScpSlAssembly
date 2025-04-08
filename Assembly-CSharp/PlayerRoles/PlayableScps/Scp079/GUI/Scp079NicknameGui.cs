using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079NicknameGui : Scp079GuiElementBase
	{
		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._camSync);
		}

		private Scp079CurrentCameraSync _camSync;
	}
}
