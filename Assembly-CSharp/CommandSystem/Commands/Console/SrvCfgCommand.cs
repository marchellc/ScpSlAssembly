using System;
using GameCore;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class SrvCfgCommand : ICommand
	{
		public string Command { get; } = "srvcfg";

		public string[] Aliases { get; }

		public string Description { get; } = "Displays the server config.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			global::GameCore.Console.singleton.TypeCommand(".srvcfg", null);
			response = null;
			return true;
		}
	}
}
