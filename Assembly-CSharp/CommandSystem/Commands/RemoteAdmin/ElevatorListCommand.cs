using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using NorthwoodLib.Pools;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(ElevatorCommand))]
	public class ElevatorListCommand : ICommand
	{
		public string Command { get; } = "list";

		public string[] Aliases { get; } = new string[] { "ls", "lst", "elevators", "lifts", "els", "elevs" };

		public string Description { get; } = "Lists all elevators.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			bool flag = arguments.Count > 0 && (arguments.At(0).Equals("detailed", StringComparison.OrdinalIgnoreCase) || arguments.At(0).Equals("d", StringComparison.OrdinalIgnoreCase) || arguments.At(0).Equals("det", StringComparison.OrdinalIgnoreCase));
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append("Detected the following elevators:");
			foreach (ElevatorGroup elevatorGroup in EnumUtils<ElevatorGroup>.Values)
			{
				stringBuilder.Append("\n- ");
				ElevatorListCommand.AppendElevatorData(elevatorGroup, flag, stringBuilder);
			}
			response = stringBuilder.ToString();
			StringBuilderPool.Shared.Return(stringBuilder);
			return true;
		}

		private static void AppendElevatorData(ElevatorGroup group, bool getLevels, StringBuilder sb)
		{
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(group, out elevatorChamber))
			{
				sb.AppendFormat("Elevator \"{0}\" does not exist.", group);
				return;
			}
			List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(group);
			if (doorsForGroup.Count == 0)
			{
				sb.AppendFormat("Elevator \"{0}\" has 0 levels.", group);
				return;
			}
			sb.AppendFormat("Elevator \"{0}\" has {1} levels. Currently ", group, doorsForGroup.Count);
			ElevatorDoor elevatorDoor = doorsForGroup.FirstOrDefault((ElevatorDoor x) => x.TargetState);
			if (elevatorDoor == null)
			{
				sb.Append("in transit");
			}
			else
			{
				sb.AppendFormat("at level {0}", doorsForGroup.IndexOf(elevatorDoor));
			}
			bool flag = false;
			if (elevatorChamber.DynamicAdminLock)
			{
				sb.Append(" and dynamic administrative lock is active");
			}
			else if (elevatorChamber.ActiveLocksAllDoors.HasFlagFast(DoorLockReason.AdminCommand))
			{
				sb.Append(" and administratively locked");
				flag = true;
			}
			else if (elevatorChamber.ActiveLocksAnyDoors.HasFlagFast(DoorLockReason.AdminCommand))
			{
				sb.Append(" and PARTIALLY administratively locked");
			}
			else if (elevatorChamber.ActiveLocksAllDoors != DoorLockReason.None)
			{
				sb.Append(" and locked");
				flag = true;
			}
			else if (elevatorChamber.ActiveLocksAnyDoors != DoorLockReason.None)
			{
				sb.Append(" and PARTIALLY locked");
			}
			sb.Append(".");
			if (!getLevels)
			{
				return;
			}
			for (int i = 0; i < doorsForGroup.Count; i++)
			{
				Vector3 position = doorsForGroup[i].transform.position;
				sb.AppendFormat("\n-   Level {0} at height {1}", i, Mathf.Round(position.y));
				RoomIdentifier roomIdentifier;
				if (RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(position), out roomIdentifier))
				{
					sb.AppendFormat(" (room: \"{0}\")", roomIdentifier.Name);
				}
				if (doorsForGroup[i].ActiveLocks != 0 && !flag)
				{
					sb.Append(((DoorLockReason)doorsForGroup[i].ActiveLocks).HasFlagFast(DoorLockReason.AdminCommand) ? " (administratively locked)" : " (locked)");
				}
			}
			sb.Append('\n');
		}
	}
}
