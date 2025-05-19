using PlayerRoles.PlayableScps.Scp079.Cameras;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079NicknameGui : Scp079GuiElementBase
{
	private Scp079CurrentCameraSync _camSync;

	internal override void Init(Scp079Role role, ReferenceHub owner)
	{
		base.Init(role, owner);
		role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out _camSync);
	}
}
