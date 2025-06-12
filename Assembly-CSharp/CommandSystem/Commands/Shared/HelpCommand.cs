using System;
using System.Text;

namespace CommandSystem.Commands.Shared;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public class HelpCommand : ICommand, IUsageProvider
{
	private readonly StringBuilder _helpBuilder = new StringBuilder();

	private readonly ICommandHandler _commandHandler;

	public string Command { get; } = "help";

	public string[] Aliases { get; }

	public string Description { get; } = "Returns all commands with their descriptions and aliases or displays help for specified command.";

	public string[] Usage { get; } = new string[1] { "Command (Optional)" };

	public HelpCommand(ICommandHandler commandHandler)
	{
		this._commandHandler = commandHandler;
	}

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (arguments.Count == 0)
		{
			response = this.GetCommandList(this._commandHandler, "Command list:");
			return true;
		}
		if (this._commandHandler.TryGetCommand(arguments.At(0), out var command))
		{
			string text = command.Command;
			ArraySegment<string> arraySegment = arguments.Segment(1);
			ICommand command2;
			while (arraySegment.Count != 0 && command is ICommandHandler commandHandler && commandHandler.TryGetCommand(arraySegment.At(0), out command2))
			{
				arraySegment = arraySegment.Segment(1);
				command = command2;
				text = text + " " + command2.Command;
			}
			response = text + " - " + ((command is IHelpProvider helpProvider) ? helpProvider.GetHelp(arraySegment) : command.Description);
			if (command.Aliases != null && command.Aliases.Length != 0)
			{
				response = response + "\nAliases: " + string.Join(", ", command.Aliases);
			}
			if (command is ICommandHandler handler)
			{
				response += this.GetCommandList(handler, "\nSubcommand list:");
			}
			try
			{
				Type type = command.GetType();
				if (type != null)
				{
					response = response + "\nImplemented in: " + type.Assembly.GetName().Name + ":" + type.FullName;
				}
			}
			catch
			{
			}
			return true;
		}
		response = "Help for " + arguments.At(0) + " isn't available!";
		return false;
	}

	private string GetCommandList(ICommandHandler handler, string header)
	{
		this._helpBuilder.Clear();
		this._helpBuilder.Append(header);
		foreach (ICommand allCommand in handler.AllCommands)
		{
			if (!(allCommand is IHiddenCommand))
			{
				this._helpBuilder.AppendLine();
				this._helpBuilder.Append(allCommand.Command);
				this._helpBuilder.Append(" - ");
				this._helpBuilder.Append(allCommand.Description);
				if (allCommand.Aliases != null && allCommand.Aliases.Length != 0)
				{
					this._helpBuilder.Append(" - Aliases: ");
					this._helpBuilder.Append(string.Join(", ", allCommand.Aliases));
				}
			}
		}
		return this._helpBuilder.ToString();
	}
}
