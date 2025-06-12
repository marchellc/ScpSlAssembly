using _Scripts.CommandSystem.Commands.Console;
using _Scripts.CommandSystem.Commands.Shared;
using CommandSystem.Commands.Console;
using CommandSystem.Commands.RemoteAdmin.Stripdown;
using CommandSystem.Commands.Shared;

namespace CommandSystem;

public class GameConsoleCommandHandler : CommandHandler
{
	private GameConsoleCommandHandler()
	{
	}

	public static GameConsoleCommandHandler Create()
	{
		GameConsoleCommandHandler gameConsoleCommandHandler = new GameConsoleCommandHandler();
		gameConsoleCommandHandler.LoadGeneratedCommands();
		return gameConsoleCommandHandler;
	}

	public override void LoadGeneratedCommands()
	{
		this.RegisterCommand(new QueryCommand());
		this.RegisterCommand(new BufferSizeCommand());
		this.RegisterCommand(new PrrCommand());
		this.RegisterCommand(new AudioPoolDebug());
		this.RegisterCommand(new BuildInfoCommand());
		this.RegisterCommand(ConfigCommand.Create());
		this.RegisterCommand(new EchoCommand());
		this.RegisterCommand(new ForceStartCommand());
		this.RegisterCommand(new HelloCommand());
		this.RegisterCommand(new HelpCommand(this));
		this.RegisterCommand(new NextRoundCommand());
		this.RegisterCommand(new NoopCommand());
		this.RegisterCommand(new PermCommand());
		this.RegisterCommand(new PlayersCommand());
		this.RegisterCommand(new QuitCommand());
		this.RegisterCommand(new RedirectCommand());
		this.RegisterCommand(new RefreshCommandsCommand(this));
		this.RegisterCommand(new RestartNextRoundCommand());
		this.RegisterCommand(new RidListCommand());
		this.RegisterCommand(new SoftRestartCommand());
		this.RegisterCommand(new StopNextRoundCommand());
		this.RegisterCommand(new SubscribeCommand());
		this.RegisterCommand(new UnsubscribeCommand());
		this.RegisterCommand(new UptimeRoundsCommand());
		this.RegisterCommand(StripdownCommand.Create());
		this.RegisterCommand(new AccuracyStatsCommand());
		this.RegisterCommand(new ArgsCommand());
		this.RegisterCommand(LineCommand.Create());
		this.RegisterCommand(new DisconnectCommand());
		this.RegisterCommand(new IdCommand());
		this.RegisterCommand(new IdleCommand());
		this.RegisterCommand(new IpCommand());
		this.RegisterCommand(new ItemListCommand());
		this.RegisterCommand(new LennyCommand());
		this.RegisterCommand(new PauseCullingCommand());
		this.RegisterCommand(new ReloadTranslationsCommand());
		this.RegisterCommand(new RoleListCommand());
		this.RegisterCommand(new SeedCommand());
		this.RegisterCommand(new CommandSystem.Commands.Console.SrvCfgCommand());
		this.RegisterCommand(new PocketDimensionCommand());
		this.RegisterCommand(new RoundRestartCommand());
	}
}
