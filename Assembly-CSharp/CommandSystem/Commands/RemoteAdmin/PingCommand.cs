using System;
using Mirror.LiteNetLib4Mirror;
using RemoteAdmin;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class PingCommand : ICommand
	{
		public string Command { get; } = "ping";

		public string[] Aliases { get; }

		public string Description { get; } = "Pong!";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			int count = arguments.Count;
			if (count != 0)
			{
				if (count != 1)
				{
					response = "Too many arguments! (expected 0 or 1)";
					return false;
				}
				if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
				{
					return false;
				}
				int num;
				if (!int.TryParse(arguments.At(0), out num))
				{
					response = "Invalid player id!";
					return false;
				}
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHub(num, out referenceHub))
				{
					response = "Invalid player id!";
					return false;
				}
				int connectionId = referenceHub.networkIdentity.connectionToClient.connectionId;
				if (connectionId == 0)
				{
					response = "This command is not available for the host!";
					return false;
				}
				response = string.Format("Ping: {0}ms", LiteNetLib4MirrorServer.Peers[connectionId].Ping * 2);
				return true;
			}
			else
			{
				PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
				if (playerCommandSender == null)
				{
					response = "This command is only available for players!";
					return false;
				}
				int connectionId2 = playerCommandSender.ReferenceHub.networkIdentity.connectionToClient.connectionId;
				if (connectionId2 == 0)
				{
					response = "This command is not available for the host!";
					return false;
				}
				response = string.Format("Your ping: {0}ms", LiteNetLib4MirrorServer.Peers[connectionId2].Ping * 2);
				return true;
			}
		}
	}
}
