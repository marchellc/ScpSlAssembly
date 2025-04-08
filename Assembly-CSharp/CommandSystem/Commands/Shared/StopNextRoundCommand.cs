using System;
using Mirror;
using ServerOutput;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class StopNextRoundCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "stopnextround";

		public string[] Aliases { get; } = new string[] { "snr" };

		public string Description { get; } = "Stops the server after the next round.";

		public string[] Usage { get; } = new string[] { "Port number to redirect players (Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			CommandSender commandSender = sender as CommandSender;
			if (commandSender != null && !commandSender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			if (!NetworkServer.active)
			{
				response = "This command can only be used on a server.";
				return false;
			}
			if (!ServerStatic.IsDedicated)
			{
				response = "This command can be only executed on a dedicated servers.";
				return false;
			}
			if (arguments.Count > 0)
			{
				ushort num;
				if (!ushort.TryParse(arguments.At(0), out num))
				{
					response = "First argument, if set, must be a valid port number.";
					return false;
				}
				if (num == ServerStatic.ShutdownRedirectPort)
				{
					response = string.Format("Server is already set to redirect players to port {0}.", num);
					return true;
				}
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} set the server shutdown redirection port to {1}.", sender.LogName, num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				ServerStatic.ShutdownRedirectPort = num;
			}
			else if (ServerStatic.ShutdownRedirectPort != 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " cleared the server shutdown redirection port.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				ServerStatic.ShutdownRedirectPort = 0;
			}
			if (ServerStatic.StopNextRound == ServerStatic.NextRoundAction.Shutdown && ServerStatic.ShutdownRedirectPort == 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " canceled server stop after the round end.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				ServerStatic.StopNextRound = ServerStatic.NextRoundAction.DoNothing;
				ServerConsole.AddOutputEntry(default(ExitActionResetEntry));
				response = "Server WON'T stop after next round.";
			}
			else
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " scheduled server stop after the round end.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Shutdown;
				ServerConsole.AddOutputEntry(default(ExitActionShutdownEntry));
				response = "Server WILL stop after next round" + ((ServerStatic.ShutdownRedirectPort == 0) ? "" : string.Format(" and players will be redirected to port {0}", ServerStatic.ShutdownRedirectPort)) + ".";
			}
			return true;
		}
	}
}
