using System;

namespace CommandSystem.Commands.Console;

[CommandHandler(typeof(GameConsoleCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class LineCommand : ParentCommand, IUsageProvider
{
	public override string Command { get; } = "line";

	public override string[] Aliases { get; } = new string[4] { "debugline", "drawline", "drawableline", "lines" };

	public override string Description { get; } = "Controls or displays information about the Wave system.";

	public string[] Usage { get; } = new string[2] { "help/toggle/test/duration", "[value]" };

	public static LineCommand Create()
	{
		LineCommand lineCommand = new LineCommand();
		lineCommand.LoadGeneratedCommands();
		return lineCommand;
	}

	protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = "Unknown subcommand.\nUsage: " + this.Command + " " + this.DisplayCommandUsage() + ".";
		return false;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new LineDurationCommand());
		this.RegisterCommand(new LineHelpCommand());
		this.RegisterCommand(new LineTestCommand());
		this.RegisterCommand(new LineToggleCommand());
	}
}
