using System;
using Query;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class DisconnectCommand : ICommand
	{
		public string Command { get; } = "disconnect";

		public string[] Aliases { get; } = new string[] { "dc" };

		public string Description { get; } = "Disconnect from server.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			QueryCommandSender queryCommandSender = sender as QueryCommandSender;
			if (queryCommandSender != null)
			{
				queryCommandSender.Disconnect();
				response = "";
				return true;
			}
			response = "This command can be only executed on a game client or server query client!";
			return false;
		}
	}
}
