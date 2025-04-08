using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem.Commands.RemoteAdmin.Doors;
using Interactables.Interobjects;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(ElevatorCommand))]
	public class ElevatorTeleportCommand : ICommand
	{
		public string Command { get; } = "teleport";

		public string[] Aliases { get; } = new string[] { "t", "tp", "goto", "tele" };

		public string Description { get; } = "Teleports to an elevator.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "Syntax error: elevator teleport <Elevator ID> <Target Players> [\"inside\"/level ID/\"outside\"]";
				return false;
			}
			ElevatorGroup elevatorGroup;
			if (!ElevatorCommand.TryParseGroup(arguments.At(0), out elevatorGroup))
			{
				response = "Elevator \"" + arguments.At(0) + "\" not found.";
				return false;
			}
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(elevatorGroup, out elevatorChamber))
			{
				response = string.Format("Elevator \"{0}\" could not be found in the Facility.", elevatorGroup);
				return false;
			}
			List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(elevatorGroup);
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 1, out array, false);
			if (list == null || list.Count == 0)
			{
				response = "No players have been selected.";
				return false;
			}
			Vector3 vector = elevatorChamber.transform.position + Vector3.up * ElevatorTeleportCommand.PositionOffset;
			string text = "inside";
			if (array != null && array.Length > 0)
			{
				string text2 = array[0].ToLowerInvariant();
				uint num = <PrivateImplementationDetails>.ComputeStringHash(text2);
				if (num <= 2870621791U)
				{
					if (num != 1094220446U)
					{
						if (num != 2565440279U)
						{
							if (num != 2870621791U)
							{
								goto IL_01E0;
							}
							if (!(text2 == "out"))
							{
								goto IL_01E0;
							}
						}
						else
						{
							if (!(text2 == "ins"))
							{
								goto IL_01E0;
							}
							goto IL_024C;
						}
					}
					else
					{
						if (!(text2 == "in"))
						{
							goto IL_01E0;
						}
						goto IL_024C;
					}
				}
				else if (num <= 3926667934U)
				{
					if (num != 3280923844U)
					{
						if (num != 3926667934U)
						{
							goto IL_01E0;
						}
						if (!(text2 == "o"))
						{
							goto IL_01E0;
						}
					}
					else if (!(text2 == "outside"))
					{
						goto IL_01E0;
					}
				}
				else if (num != 3960223172U)
				{
					if (num != 4148812653U)
					{
						goto IL_01E0;
					}
					if (!(text2 == "inside"))
					{
						goto IL_01E0;
					}
					goto IL_024C;
				}
				else
				{
					if (!(text2 == "i"))
					{
						goto IL_01E0;
					}
					goto IL_024C;
				}
				vector = DoorTPCommand.EnsurePositionSafety(elevatorChamber.DestinationDoor.transform);
				text = "outside";
				goto IL_024C;
				IL_01E0:
				int num2;
				if (!int.TryParse(array[0], out num2))
				{
					response = "Invalid level ID: " + array[0];
					return false;
				}
				if (num2 < 0 || num2 >= doorsForGroup.Count)
				{
					response = "Selected elevator doesn't have level " + array[0] + ".";
					return false;
				}
				vector = DoorTPCommand.EnsurePositionSafety(doorsForGroup[num2].transform);
				text = string.Format("level {0}", num2);
			}
			IL_024C:
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num3 = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (referenceHub.TryOverridePosition(vector))
				{
					if (num3 != 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
					num3++;
				}
			}
			if (num3 > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} teleported player{1}{2} to elevator {3} ({4}).", new object[]
				{
					sender.LogName,
					(num3 == 1) ? " " : "s ",
					stringBuilder,
					elevatorGroup.ToString(),
					text
				}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num3, (num3 == 1) ? "!" : "s!");
			return true;
		}

		private static float PositionOffset = 0.8f;
	}
}
