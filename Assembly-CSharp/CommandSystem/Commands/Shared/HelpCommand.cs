using System;
using System.Text;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	[CommandHandler(typeof(ClientCommandHandler))]
	public class HelpCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "help";

		public string[] Aliases { get; }

		public string Description { get; } = "Returns all commands with their descriptions and aliases or displays help for specified command.";

		public string[] Usage { get; } = new string[] { "Command (Optional)" };

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
			ICommand command;
			if (this._commandHandler.TryGetCommand(arguments.At(0), out command))
			{
				string text = command.Command;
				ArraySegment<string> arraySegment = arguments.Segment(1);
				while (arraySegment.Count != 0)
				{
					ICommandHandler commandHandler = command as ICommandHandler;
					ICommand command2;
					if (commandHandler == null || !commandHandler.TryGetCommand(arraySegment.At(0), out command2))
					{
						break;
					}
					arraySegment = arraySegment.Segment(1);
					command = command2;
					text = text + " " + command2.Command;
				}
				string text2 = text;
				string text3 = " - ";
				IHelpProvider helpProvider = command as IHelpProvider;
				response = text2 + text3 + ((helpProvider != null) ? helpProvider.GetHelp(arraySegment) : command.Description);
				if (command.Aliases != null && command.Aliases.Length != 0)
				{
					response = response + "\nAliases: " + string.Join(", ", command.Aliases);
				}
				ICommandHandler commandHandler2 = command as ICommandHandler;
				if (commandHandler2 != null)
				{
					response += this.GetCommandList(commandHandler2, "\nSubcommand list:");
				}
				try
				{
					Type type = command.GetType();
					if (type != null)
					{
						response = string.Concat(new string[]
						{
							response,
							"\nImplemented in: ",
							type.Assembly.GetName().Name,
							":",
							type.FullName
						});
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
			foreach (ICommand command in handler.AllCommands)
			{
				if (!(command is IHiddenCommand))
				{
					this._helpBuilder.AppendLine();
					this._helpBuilder.Append(command.Command);
					this._helpBuilder.Append(" - ");
					this._helpBuilder.Append(command.Description);
					if (command.Aliases != null && command.Aliases.Length != 0)
					{
						this._helpBuilder.Append(" - Aliases: ");
						this._helpBuilder.Append(string.Join(", ", command.Aliases));
					}
				}
			}
			return this._helpBuilder.ToString();
		}

		private readonly StringBuilder _helpBuilder = new StringBuilder();

		private readonly ICommandHandler _commandHandler;
	}
}
