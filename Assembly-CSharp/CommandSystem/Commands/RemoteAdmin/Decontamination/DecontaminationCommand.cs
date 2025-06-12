using System;

namespace CommandSystem.Commands.RemoteAdmin.Decontamination;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class DecontaminationCommand : ParentCommand, IUsageProvider
{
	public override string Command { get; } = "decontamination";

	public override string[] Aliases { get; } = new string[1] { "decont" };

	public override string Description { get; } = "Controls the LCZ decontamination.";

	public string[] Usage { get; } = new string[1] { "status/enable/disable" };

	public static DecontaminationCommand Create()
	{
		DecontaminationCommand decontaminationCommand = new DecontaminationCommand();
		decontaminationCommand.LoadGeneratedCommands();
		return decontaminationCommand;
	}

	protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = "Please specify a valid subcommand (" + this.Usage[0] + ")!";
		return false;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new DisableCommand());
		this.RegisterCommand(new EnableCommand());
		this.RegisterCommand(new ForceCommand());
		this.RegisterCommand(new StatusCommand());
	}
}
