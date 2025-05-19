using System;

namespace Respawning;

[Flags]
public enum UpdateMessageFlags : byte
{
	None = 0,
	Timer = 1,
	Pause = 2,
	Trigger = 4,
	Tokens = 8,
	Spawn = 0x10,
	All = 0xB
}
