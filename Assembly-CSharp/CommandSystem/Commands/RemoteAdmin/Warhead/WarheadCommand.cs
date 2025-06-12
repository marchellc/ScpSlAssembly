using System;

namespace CommandSystem.Commands.RemoteAdmin.Warhead;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class WarheadCommand : ParentCommand, IUsageProvider
{
	public override string Command { get; } = "warhead";

	public override string[] Aliases { get; } = new string[1] { "wh" };

	public override string Description { get; } = "Manages the alpha warhead.";

	public string[] Usage { get; } = new string[1] { "SubCommand" };

	public static WarheadCommand Create()
	{
		WarheadCommand warheadCommand = new WarheadCommand();
		warheadCommand.LoadGeneratedCommands();
		return warheadCommand;
	}

	protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = "Please specify a valid subcommand!";
		return false;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new CancelCommand());
		this.RegisterCommand(new DetonateCommand());
		this.RegisterCommand(new DisableCommand());
		this.RegisterCommand(new EnableCommand());
		this.RegisterCommand(new InstantCommand());
		this.RegisterCommand(new LockCommand());
		this.RegisterCommand(new SetTimeCommand());
		this.RegisterCommand(new StatusCommand());
	}
}
