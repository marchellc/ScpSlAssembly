using System;

namespace Interactables.Interobjects.DoorUtils
{
	[Flags]
	public enum DoorLockReason : ushort
	{
		None = 0,
		Regular079 = 1,
		Lockdown079 = 2,
		Warhead = 4,
		AdminCommand = 8,
		DecontLockdown = 16,
		DecontEvacuate = 32,
		SpecialDoorFeature = 64,
		NoPower = 128,
		Isolation = 256,
		Lockdown2176 = 512
	}
}
