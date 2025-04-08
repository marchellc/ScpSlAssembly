using System;
using System.Collections.Generic;
using InventorySystem;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public static class DoorScheduledUnlocker
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnUpdate += DoorScheduledUnlocker.UnityStaticUpdate;
			Inventory.OnServerStarted += DoorScheduledUnlocker.Entries.Clear;
			Inventory.OnServerStarted += DoorScheduledUnlocker.EntriesToRemove.Clear;
		}

		private static void UnityStaticUpdate()
		{
			if (!StaticUnityMethods.IsPlaying)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < DoorScheduledUnlocker.Entries.Count; i++)
			{
				if (DoorScheduledUnlocker.Entries[i].Refresh())
				{
					flag = true;
					DoorScheduledUnlocker.EntriesToRemove.Add(i);
				}
			}
			if (!flag)
			{
				return;
			}
			for (int j = 0; j < DoorScheduledUnlocker.EntriesToRemove.Count; j++)
			{
				DoorScheduledUnlocker.Entries.RemoveAt(DoorScheduledUnlocker.EntriesToRemove[j] - j);
			}
			DoorScheduledUnlocker.EntriesToRemove.Clear();
		}

		public static void UnlockLater(this DoorVariant door, float time, DoorLockReason flagsToUnlock)
		{
			DoorScheduledUnlocker.ScheduledUnlock scheduledUnlock = new DoorScheduledUnlocker.ScheduledUnlock(door, time, flagsToUnlock);
			for (int i = 0; i < DoorScheduledUnlocker.Entries.Count; i++)
			{
				if (!(DoorScheduledUnlocker.Entries[i].Door != door))
				{
					DoorScheduledUnlocker.Entries[i] = scheduledUnlock;
				}
			}
			DoorScheduledUnlocker.Entries.Add(scheduledUnlock);
		}

		private static readonly List<DoorScheduledUnlocker.ScheduledUnlock> Entries = new List<DoorScheduledUnlocker.ScheduledUnlock>();

		private static readonly List<int> EntriesToRemove = new List<int>();

		private class ScheduledUnlock
		{
			public ScheduledUnlock(DoorVariant targetDoor, float time, DoorLockReason flags)
			{
				this.Door = targetDoor;
				this._targetTime = Time.timeSinceLevelLoad + time;
				this._flagsToUnset = flags;
			}

			public bool Refresh()
			{
				if (Time.timeSinceLevelLoad < this._targetTime)
				{
					return false;
				}
				if (this.Door != null)
				{
					this.Door.ServerChangeLock(this._flagsToUnset, false);
				}
				return true;
			}

			public readonly DoorVariant Door;

			private float _targetTime;

			private readonly DoorLockReason _flagsToUnset;
		}
	}
}
