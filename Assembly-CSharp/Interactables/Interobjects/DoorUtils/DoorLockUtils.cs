using System;

namespace Interactables.Interobjects.DoorUtils
{
	public static class DoorLockUtils
	{
		public static DoorLockMode GetMode(DoorLockReason reason)
		{
			if (reason == DoorLockReason.None)
			{
				return DoorLockMode.CanOpen | DoorLockMode.CanClose;
			}
			if ((reason & (DoorLockReason.AdminCommand | DoorLockReason.DecontLockdown | DoorLockReason.SpecialDoorFeature | DoorLockReason.NoPower | DoorLockReason.Lockdown2176)) > DoorLockReason.None)
			{
				return DoorLockMode.FullLock;
			}
			if ((reason & (DoorLockReason.Regular079 | DoorLockReason.Lockdown079)) > DoorLockReason.None)
			{
				return DoorLockMode.ScpOverride;
			}
			if ((reason & (DoorLockReason.Warhead | DoorLockReason.DecontEvacuate)) > DoorLockReason.None)
			{
				return DoorLockMode.CanOpen;
			}
			if ((reason & DoorLockReason.Isolation) > DoorLockReason.None)
			{
				return DoorLockMode.CanClose;
			}
			return DoorLockMode.CanOpen | DoorLockMode.CanClose;
		}

		public static DoorLockMode GetMode(DoorVariant door)
		{
			return DoorLockUtils.GetMode((DoorLockReason)door.ActiveLocks);
		}

		public static bool HasFlagFast(this DoorLockMode mode, DoorLockMode flag)
		{
			return (mode & flag) == flag;
		}

		public static bool HasFlagFast(this DoorLockReason res, DoorLockReason flag)
		{
			return (res & flag) == flag;
		}
	}
}
