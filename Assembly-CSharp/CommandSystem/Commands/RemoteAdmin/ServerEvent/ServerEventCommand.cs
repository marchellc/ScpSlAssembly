using System;

namespace CommandSystem.Commands.RemoteAdmin.ServerEvent;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class ServerEventCommand : ParentCommand, IUsageProvider
{
	public override string Command { get; } = "SERVER_EVENT";

	public override string[] Aliases { get; }

	public override string Description { get; } = "Various server event controls";

	public string[] Usage { get; } = new string[1] { "SubCommand" };

	public static ServerEventCommand Create()
	{
		ServerEventCommand serverEventCommand = new ServerEventCommand();
		serverEventCommand.LoadGeneratedCommands();
		return serverEventCommand;
	}

	protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		response = "Invalid subcommand!";
		return false;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new DetonationCancelCommand());
		this.RegisterCommand(new DetonationInstantCommand());
		this.RegisterCommand(new DetonationStartCommand());
		this.RegisterCommand(new RoundRestartCommand());
		this.RegisterCommand(new TerminateUnconnectedCommand());
	}
}
