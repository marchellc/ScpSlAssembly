using System;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class UnsubscribeCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "unsubscribe";

		public string[] Aliases { get; } = new string[] { "unsub" };

		public string Description { get; } = "Unsubscribes you from a data feed.";

		public string[] Usage { get; } = new string[] { "console/log/all" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			IOutput output = sender as IOutput;
			if (output == null)
			{
				response = "You can't unsubscribe from a feed.";
				return false;
			}
			if (arguments.Count != 1)
			{
				response = "Usage: unsubscribe <feed>\nAvailable feeds: console/log/all";
				return false;
			}
			string text = arguments.At(0).ToLowerInvariant();
			if (!(text == "console"))
			{
				if (!(text == "log"))
				{
					if (!(text == "all"))
					{
						response = "Invalid feed name.";
						return false;
					}
					IOutput output2;
					ServerConsole.ConsoleOutputs.TryRemove(output.OutputId, out output2);
					ServerLogs.LiveLogOutput.TryRemove(output.OutputId, out output2);
					response = "Unsubscribed from all feeds.";
					return true;
				}
				else if (ServerLogs.LiveLogOutput.ContainsKey(output.OutputId))
				{
					IOutput output2;
					ServerLogs.LiveLogOutput.TryRemove(output.OutputId, out output2);
					response = "Unsubscribed from log feed.";
					return true;
				}
			}
			else if (ServerConsole.ConsoleOutputs.ContainsKey(output.OutputId))
			{
				IOutput output2;
				ServerConsole.ConsoleOutputs.TryRemove(output.OutputId, out output2);
				response = "Unsubscribed from console feed.";
				return true;
			}
			response = "You are not subscribed to that feed.";
			return false;
		}
	}
}
