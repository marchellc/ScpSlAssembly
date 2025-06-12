using System;

namespace CommandSystem.Commands.Shared;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class RefreshCommandsCommand : ICommand
{
	private readonly ICommandHandler _commandHandler;

	public string Command { get; } = "refreshcommands";

	public string[] Aliases { get; }

	public string Description { get; } = "Reloads all commands.";

	public RefreshCommandsCommand(ICommandHandler commandHandler)
	{
		this._commandHandler = commandHandler;
	}

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
		{
			return false;
		}
		this._commandHandler.ClearCommands();
		this._commandHandler.LoadGeneratedCommands();
		response = "Successfully reloaded all commands!";
		return true;
	}
}
