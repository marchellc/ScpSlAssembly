using System;
using Mirror;
using RoundRestarting;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class SoftRestartCommand : ICommand
	{
		public string Command { get; } = "softrestart";

		public string[] Aliases { get; } = new string[] { "srestart", "sr" };

		public string Description { get; } = "Restarts the server, but tells all the players to reconnect after the restart.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!NetworkServer.active)
			{
				response = "This command can only be used on a server.";
				return false;
			}
			CommandSender commandSender = sender as CommandSender;
			if (commandSender != null && !commandSender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			if (!ServerStatic.IsDedicated)
			{
				response = "This command can be only executed on a dedicated servers.";
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " performed a soft server restart.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
			RoundRestart.ChangeLevel(true);
			response = "Server will softly restart in a couple of seconds.";
			return true;
		}
	}
}
