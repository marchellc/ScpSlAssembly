﻿using System;

namespace Interactables.Interobjects.DoorUtils
{
	[Flags]
	public enum DoorDamageType : byte
	{
		None = 1,
		ServerCommand = 2,
		Grenade = 4,
		Weapon = 8,
		Scp096 = 16,
		ParticleDisruptor = 32
	}
}
