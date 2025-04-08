using System;

namespace GameCore
{
	public enum ConfigShareTypes : byte
	{
		Bans,
		Mutes,
		Whitelist,
		ReservedSlots,
		Groups,
		GroupsMembers,
		GameplayDatabase
	}
}
