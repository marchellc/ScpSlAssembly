using System;
using AudioPooling;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	[CommandHandler(typeof(ClientCommandHandler))]
	public class AudioPoolDebug : ICommand, IUsageProvider
	{
		public string Command { get; } = "audiopooldebug";

		public string[] Aliases { get; } = new string[] { "audiopoolingdebug" };

		public string Description { get; } = "Prints debug information about Audio Pooling system.";

		public string[] Usage { get; } = new string[] { "Force refresh (bool, Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			bool flag2;
			bool flag = arguments.Count > 0 && bool.TryParse(arguments.At(0), out flag2) && flag2;
			response = AudioSourcePoolManager.ProcessConsoleCommand(flag);
			return true;
		}
	}
}
