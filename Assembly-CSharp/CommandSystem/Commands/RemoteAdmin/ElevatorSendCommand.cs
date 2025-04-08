using System;
using System.Collections.Generic;
using System.Text;
using Interactables.Interobjects;
using NorthwoodLib.Pools;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(ElevatorCommand))]
	public class ElevatorSendCommand : ICommand
	{
		public string Command { get; } = "send";

		public string[] Aliases { get; } = new string[] { "s", "snd" };

		public string Description { get; } = "Sends an elevator.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1 || arguments.Count > 2)
			{
				response = "Syntax error: elevator send <Elevator ID / \"all\"> [level]";
				return false;
			}
			string text = arguments.At(0);
			bool flag = text.Equals("all", StringComparison.OrdinalIgnoreCase) || text.Equals("*", StringComparison.OrdinalIgnoreCase);
			int num = -1;
			if (arguments.Count > 1 && (!int.TryParse(arguments.At(1), out num) || num < 0))
			{
				response = "Level must be a nonnegative integer.";
				return false;
			}
			if (flag)
			{
				StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
				bool flag2 = true;
				bool flag3;
				try
				{
					ElevatorGroup[] values = EnumUtils<ElevatorGroup>.Values;
					for (int i = 0; i < values.Length; i++)
					{
						string text2;
						if (!ElevatorSendCommand.SendElevator(values[i], -1, out text2, sender))
						{
							flag2 = false;
						}
						stringBuilder.AppendFormat("{0}\n", text2);
					}
					response = stringBuilder.ToString();
					flag3 = flag2;
				}
				finally
				{
					StringBuilderPool.Shared.Return(stringBuilder);
				}
				return flag3;
			}
			ElevatorGroup elevatorGroup;
			if (!ElevatorCommand.TryParseGroup(text, out elevatorGroup))
			{
				response = "Elevator \"" + text + "\" not found.";
				return false;
			}
			return ElevatorSendCommand.SendElevator(elevatorGroup, num, out response, sender);
		}

		private static bool SendElevator(ElevatorGroup group, int level, out string response, ICommandSender sender)
		{
			List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(group);
			if (doorsForGroup.Count == 0)
			{
				response = string.Format("No doors for \"{0}\" could not be found in the Facility.", group);
				return false;
			}
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(group, out elevatorChamber))
			{
				response = string.Format("Chamber for elevator \"{0}\" is not spawned.", group);
				return false;
			}
			if (level == -1)
			{
				level = elevatorChamber.NextLevel;
			}
			else if (level >= doorsForGroup.Count)
			{
				response = string.Format("Elevator \"{0}\" does not have a level {1}.", group, level);
				return false;
			}
			elevatorChamber.ServerSetDestination(level, true);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} sent elevator {1} to level {2}.", sender.LogName, group, level), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = string.Format("Elevator \"{0}\" has been sent to level {1}.", group, level);
			return true;
		}
	}
}
