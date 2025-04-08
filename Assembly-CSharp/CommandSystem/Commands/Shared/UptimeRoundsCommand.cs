using System;
using RoundRestarting;
using UnityEngine;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class UptimeRoundsCommand : ICommand
	{
		public string Command { get; } = "uptime";

		public string[] Aliases { get; } = new string[] { "rounds" };

		public string Description { get; } = "Displays the uptime of the game.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = string.Format("Server uptime: {0} seconds, {1} rounds.", Time.unscaledTime, RoundRestart.UptimeRounds);
			return true;
		}
	}
}
