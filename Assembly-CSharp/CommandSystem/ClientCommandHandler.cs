using CommandSystem.Commands.Dot.Overwatch;
using CommandSystem.Commands.Shared;

namespace CommandSystem;

public class ClientCommandHandler : CommandHandler
{
	private ClientCommandHandler()
	{
	}

	public static ClientCommandHandler Create()
	{
		ClientCommandHandler clientCommandHandler = new ClientCommandHandler();
		clientCommandHandler.LoadGeneratedCommands();
		return clientCommandHandler;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new AudioPoolDebug());
		this.RegisterCommand(new ContactCommand());
		this.RegisterCommand(new EchoCommand());
		this.RegisterCommand(new GlobalTagCommand());
		this.RegisterCommand(new GroupsCommand());
		this.RegisterCommand(new HelloCommand());
		this.RegisterCommand(new HelpCommand(this));
		this.RegisterCommand(new HideTagCommand());
		this.RegisterCommand(new NoopCommand());
		this.RegisterCommand(new ShowTagCommand());
		this.RegisterCommand(new SrvCfgCommand());
		this.RegisterCommand(OverwatchCommand.Create());
	}
}
