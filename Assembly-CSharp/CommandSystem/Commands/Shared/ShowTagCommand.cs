using System;
using RemoteAdmin;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(ClientCommandHandler))]
	public class ShowTagCommand : ICommand
	{
		public string Command { get; } = "showtag";

		public string[] Aliases { get; } = new string[] { "tag", "stag", "st", "sh" };

		public string Description { get; } = "Shows your local tag.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "You must be in-game to use this command!";
				return false;
			}
			playerCommandSender.ReferenceHub.serverRoles.RefreshLocalTag();
			response = "Local tag refreshed.";
			return true;
		}
	}
}
