using System;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class SubscribeCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "subscribe";

		public string[] Aliases { get; } = new string[] { "sub" };

		public string Description { get; } = "Subscribes you to a data feed.";

		public string[] Usage { get; } = new string[] { "console/log/all" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			IOutput output = sender as IOutput;
			if (output == null)
			{
				response = "You can't subscribe to a feed.";
				return false;
			}
			if (arguments.Count != 1)
			{
				response = "Usage: subscribe <feed>\nAvailable feeds: console/log/all";
				return false;
			}
			if (!sender.CheckPermission(PlayerPermissions.ServerLogLiveFeed, out response))
			{
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
					if (sender is ServerConsoleSender)
					{
						goto IL_00B1;
					}
					if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
					{
						return false;
					}
					ServerConsole.ConsoleOutputs.TryAdd(output.OutputId, output);
					ServerLogs.LiveLogOutput.TryAdd(output.OutputId, output);
					response = "Subscribed to all feeds.";
					return true;
				}
				else if (!ServerLogs.LiveLogOutput.ContainsKey(output.OutputId))
				{
					ServerLogs.LiveLogOutput.TryAdd(output.OutputId, output);
					response = "Subscribed to log feed.";
					return true;
				}
			}
			else if (!ServerConsole.ConsoleOutputs.ContainsKey(output.OutputId))
			{
				if (sender is ServerConsoleSender)
				{
					goto IL_00B1;
				}
				if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
				{
					return false;
				}
				ServerConsole.ConsoleOutputs.TryAdd(output.OutputId, output);
				response = "Subscribed to console feed.";
				return true;
			}
			response = "You are already subscribed to this feed.";
			return false;
			IL_00B1:
			response = "Console feed is not available for server or client console.";
			return false;
		}
	}
}
