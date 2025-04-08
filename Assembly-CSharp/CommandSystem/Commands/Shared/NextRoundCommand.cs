using System;
using Mirror;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class NextRoundCommand : ICommand
	{
		public string Command { get; } = "nextround";

		public string[] Aliases { get; } = new string[] { "nr" };

		public string Description { get; } = "Manages server behavior when the current round ends.";

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
			bool flag = ServerStatic.ShutdownRedirectPort > 0;
			ServerStatic.NextRoundAction stopNextRound = ServerStatic.StopNextRound;
			string text;
			if (stopNextRound != ServerStatic.NextRoundAction.Restart)
			{
				if (stopNextRound != ServerStatic.NextRoundAction.Shutdown)
				{
					text = "Round will normally restart.";
				}
				else if (flag)
				{
					text = string.Format("Server will SHUTDOWN and all players will be redirected to port {0}.", ServerStatic.ShutdownRedirectPort);
				}
				else
				{
					text = "Server will SHUTDOWN.";
				}
			}
			else
			{
				text = "Server will RESTART.";
			}
			response = text;
			return true;
		}
	}
}
