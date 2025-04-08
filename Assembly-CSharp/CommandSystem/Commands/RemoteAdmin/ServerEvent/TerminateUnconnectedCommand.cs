using System;
using GameCore;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.ServerEvent
{
	[CommandHandler(typeof(ServerEventCommand))]
	public class TerminateUnconnectedCommand : ICommand
	{
		public string Command { get; } = "TERMINATE_UNCONN";

		public string[] Aliases { get; }

		public string Description { get; } = "Terminates unconnected players.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.RoundEvents, out response))
			{
				return false;
			}
			foreach (NetworkConnectionToClient networkConnectionToClient in NetworkServer.connections.Values)
			{
				if (global::GameCore.Console.FindConnectedRoot(networkConnectionToClient) == null)
				{
					networkConnectionToClient.Disconnect();
				}
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " terminated unconnected players.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = "Terminated unconnected players.";
			return true;
		}
	}
}
