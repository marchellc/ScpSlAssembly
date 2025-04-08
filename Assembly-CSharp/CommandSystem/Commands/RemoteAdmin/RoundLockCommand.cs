using System;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class RoundLockCommand : ICommand
	{
		public string Command { get; } = "roundlock";

		public string[] Aliases { get; } = new string[] { "rl", "rlock" };

		public string Description { get; } = "Locks or unlocks the current round (prevents it from ending).";

		public string[] Usage { get; } = new string[] { "enable/disable (Leave blank for toggle)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.RoundEvents, out response))
			{
				return false;
			}
			if (arguments.Count >= 1)
			{
				string[] array = new string[] { arguments.Array[1] };
				Misc.CommandOperationMode commandOperationMode;
				if (Misc.TryCommandModeFromArgs(ref array, out commandOperationMode))
				{
					switch (commandOperationMode)
					{
					case Misc.CommandOperationMode.Disable:
						RoundSummary.RoundLock = false;
						break;
					case Misc.CommandOperationMode.Enable:
						RoundSummary.RoundLock = true;
						break;
					case Misc.CommandOperationMode.Toggle:
						RoundSummary.RoundLock = !RoundSummary.RoundLock;
						break;
					}
					response = "Round lock set to " + (RoundSummary.RoundLock ? "enabled!" : "disabled!");
					return true;
				}
			}
			RoundSummary.RoundLock = !RoundSummary.RoundLock;
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + (RoundSummary.RoundLock ? " enabled " : " disabled ") + "round lock.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = "Round lock " + (RoundSummary.RoundLock ? "enabled!" : "disabled!");
			return true;
		}
	}
}
