using System;

[Flags]
public enum PlayerPermissions : ulong
{
	KickingAndShortTermBanning = 1UL,
	BanningUpToDay = 2UL,
	LongTermBanning = 4UL,
	ForceclassSelf = 8UL,
	ForceclassToSpectator = 16UL,
	ForceclassWithoutRestrictions = 32UL,
	GivingItems = 64UL,
	WarheadEvents = 128UL,
	RespawnEvents = 256UL,
	RoundEvents = 512UL,
	SetGroup = 1024UL,
	GameplayData = 2048UL,
	Overwatch = 4096UL,
	FacilityManagement = 8192UL,
	PlayersManagement = 16384UL,
	PermissionsManagement = 32768UL,
	ServerConsoleCommands = 65536UL,
	ViewHiddenBadges = 131072UL,
	ServerConfigs = 262144UL,
	Broadcasting = 524288UL,
	PlayerSensitiveDataAccess = 1048576UL,
	Noclip = 2097152UL,
	AFKImmunity = 4194304UL,
	AdminChat = 8388608UL,
	ViewHiddenGlobalBadges = 16777216UL,
	Announcer = 33554432UL,
	Effects = 67108864UL,
	FriendlyFireDetectorImmunity = 134217728UL,
	FriendlyFireDetectorTempDisable = 268435456UL,
	ServerLogLiveFeed = 536870912UL
}
