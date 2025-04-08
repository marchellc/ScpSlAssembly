using System;
using ProgressiveCulling;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class PauseCullingCommand : ICommand
	{
		public string Command { get; } = "pauseculling";

		public string[] Aliases { get; } = new string[] { "cullingpause" };

		public string Description { get; } = "Toggles the culling system's pause on or off.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = (CullingCamera.TogglePause() ? "Culling system paused successfully. It may become unpaused when playing on a server with active enemies." : "Culling system unpaused or failed to pause. This feature is unavailable when playing on a server with active enemies.");
			return true;
		}
	}
}
