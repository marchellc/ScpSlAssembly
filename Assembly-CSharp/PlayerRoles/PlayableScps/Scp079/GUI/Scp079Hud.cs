using PlayerRoles.PlayableScps.HUDs;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079Hud : ScpHudBase
{
	public static Camera MainCamera { get; private set; }

	public static Scp079Role Instance { get; private set; }
}
