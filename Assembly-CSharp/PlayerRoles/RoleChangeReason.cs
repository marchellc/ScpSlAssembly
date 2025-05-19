namespace PlayerRoles;

public enum RoleChangeReason : byte
{
	None,
	RoundStart,
	LateJoin,
	Respawn,
	Died,
	Escaped,
	Revived,
	RemoteAdmin,
	Destroyed,
	RespawnMiniwave,
	ItemUsage
}
