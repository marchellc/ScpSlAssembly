using System;
using System.Collections.Generic;
using System.Text;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using NorthwoodLib.Pools;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(ElevatorCommand))]
	public class ElevatorLockCommand : ICommand
	{
		public string Command { get; } = "lock";

		public string[] Aliases { get; } = new string[] { "l", "lck" };

		public string Description { get; } = "Locks an elevator.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			int count = arguments.Count;
			if (count != 1)
			{
				if (count == 2)
				{
					if (arguments.At(1).Equals("d", StringComparison.OrdinalIgnoreCase) || arguments.At(1).Equals("dynamic", StringComparison.OrdinalIgnoreCase))
					{
						return ElevatorLockCommand.TrySetLock(arguments.At(0), true, out response, sender, -2);
					}
					int num;
					if (int.TryParse(arguments.At(1), out num) && num >= 0)
					{
						return ElevatorLockCommand.TrySetLock(arguments.At(0), true, out response, sender, num);
					}
				}
				response = "Syntax error: elevator lock <Elevator ID / \"all\"> <level / \"dynamic\" (optional)>";
				return false;
			}
			return ElevatorLockCommand.TrySetLock(arguments.At(0), true, out response, sender, -1);
		}

		internal static bool TrySetLock(string elevatorId, bool locked, out string response, ICommandSender sender, int level)
		{
			if (elevatorId.Equals("all", StringComparison.OrdinalIgnoreCase) || elevatorId.Equals("*", StringComparison.Ordinal))
			{
				StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
				bool flag = true;
				bool flag2;
				try
				{
					foreach (ElevatorGroup elevatorGroup in EnumUtils<ElevatorGroup>.Values)
					{
						if (ElevatorLockCommand.SetLock(locked, sender, elevatorGroup, level))
						{
							stringBuilder.AppendFormat("Elevator \"{0}\" has been {1}.\n", elevatorGroup, locked ? "locked" : "unlocked");
						}
						else
						{
							flag = false;
							stringBuilder.AppendFormat("Could not update lock status for elevator \"{0}\".\n", elevatorGroup);
						}
					}
					response = stringBuilder.ToString();
					flag2 = flag;
				}
				finally
				{
					StringBuilderPool.Shared.Return(stringBuilder);
				}
				return flag2;
			}
			ElevatorGroup elevatorGroup2;
			if (!ElevatorCommand.TryParseGroup(elevatorId, out elevatorGroup2))
			{
				response = "Elevator \"" + elevatorId + "\" not found.";
				return false;
			}
			if (!ElevatorLockCommand.SetLock(locked, sender, elevatorGroup2, level))
			{
				response = string.Format("Could not update lock status for elevator \"{0}\".", elevatorGroup2);
				return false;
			}
			response = string.Format("Elevator \"{0}\" has been {1}.", elevatorGroup2, locked ? "locked" : "unlocked");
			return true;
		}

		private static bool SetLock(bool forceLock, ICommandSender sender, ElevatorGroup elevatorGroup, int level)
		{
			if (level < -2)
			{
				return false;
			}
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(elevatorGroup, out elevatorChamber))
			{
				return false;
			}
			List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(elevatorGroup);
			if (level == -1)
			{
				return ElevatorLockCommand.SetLockAll(doorsForGroup, forceLock, sender, elevatorGroup, elevatorChamber);
			}
			if (level == -2)
			{
				if (elevatorChamber.DynamicAdminLock)
				{
					return false;
				}
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} enabled dynamic lock for elevator {1}.", sender.LogName, elevatorGroup), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				elevatorChamber.DynamicAdminLock = true;
				return true;
			}
			else
			{
				if (doorsForGroup.Count <= level)
				{
					return false;
				}
				ElevatorDoor elevatorDoor = doorsForGroup[level];
				if (((DoorLockReason)elevatorDoor.ActiveLocks).HasFlagFast(DoorLockReason.AdminCommand) == forceLock)
				{
					return true;
				}
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} {1} level {2} of elevator {3}.", new object[]
				{
					sender.LogName,
					forceLock ? "locked" : "unlocked",
					level,
					elevatorGroup
				}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				if (elevatorChamber.DynamicAdminLock)
				{
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} disabled dynamic lock for elevator {1}.", sender.LogName, elevatorGroup), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					elevatorChamber.DynamicAdminLock = false;
				}
				if (forceLock)
				{
					ElevatorDoor elevatorDoor2 = elevatorDoor;
					elevatorDoor2.NetworkActiveLocks = elevatorDoor2.ActiveLocks | 8;
				}
				else
				{
					ElevatorDoor elevatorDoor3 = elevatorDoor;
					elevatorDoor3.NetworkActiveLocks = elevatorDoor3.ActiveLocks & 65527;
				}
				return true;
			}
		}

		private static bool SetLockAll(List<ElevatorDoor> list, bool isLocking, ICommandSender sender, ElevatorGroup elevatorGroup, ElevatorChamber targetChamber)
		{
			if (targetChamber.DynamicAdminLock)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} disabled dynamic lock for elevator {1}.", sender.LogName, elevatorGroup), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				targetChamber.DynamicAdminLock = false;
				if (!isLocking)
				{
					return true;
				}
			}
			if (isLocking && targetChamber.ActiveLocksAllDoors.HasFlagFast(DoorLockReason.AdminCommand))
			{
				return true;
			}
			if (!isLocking && !targetChamber.ActiveLocksAnyDoors.HasFlagFast(DoorLockReason.AdminCommand))
			{
				return true;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} {1} elevator {2}.", sender.LogName, isLocking ? "locked" : "unlocked", elevatorGroup), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			foreach (ElevatorDoor elevatorDoor in list)
			{
				if (isLocking)
				{
					ElevatorDoor elevatorDoor2 = elevatorDoor;
					elevatorDoor2.NetworkActiveLocks = elevatorDoor2.ActiveLocks | 8;
				}
				else
				{
					ElevatorDoor elevatorDoor3 = elevatorDoor;
					elevatorDoor3.NetworkActiveLocks = elevatorDoor3.ActiveLocks & 65527;
				}
			}
			return true;
		}

		private const int AllLevels = -1;

		private const int DynamicLock = -2;
	}
}
