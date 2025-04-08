using System;
using Respawning;

namespace CommandSystem.Commands
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class CassieClear : ICommand
	{
		public string Command { get; } = "clearcassie";

		public string[] Aliases { get; } = new string[] { "cassieclear" };

		public string Description { get; } = "Clears cassie message queue.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Announcer, out response))
			{
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " ran the clearcassie command", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			RespawnEffectsController.ClearQueue();
			response = "Cleared Cassie word queue!";
			return true;
		}
	}
}
