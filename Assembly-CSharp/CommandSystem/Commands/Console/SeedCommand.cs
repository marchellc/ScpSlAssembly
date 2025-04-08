using System;
using MapGeneration;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class SeedCommand : ICommand
	{
		public string Command { get; } = "seed";

		public string[] Aliases { get; }

		public string Description { get; } = "Displays the seed for the current round.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = string.Format("Map seed is: {0}", SeedSynchronizer.Seed);
			return true;
		}
	}
}
