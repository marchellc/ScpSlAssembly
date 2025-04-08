using System;
using Christmas.Scp2536;
using CommandSystem.Commands;
using CommandSystem.Commands.Console;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.RemoteAdmin.Broadcasts;
using CommandSystem.Commands.RemoteAdmin.Cleanup;
using CommandSystem.Commands.RemoteAdmin.Decontamination;
using CommandSystem.Commands.RemoteAdmin.Doors;
using CommandSystem.Commands.RemoteAdmin.Dummies;
using CommandSystem.Commands.RemoteAdmin.Inventory;
using CommandSystem.Commands.RemoteAdmin.MutingAndIntercom;
using CommandSystem.Commands.RemoteAdmin.ServerEvent;
using CommandSystem.Commands.RemoteAdmin.Stripdown;
using CommandSystem.Commands.RemoteAdmin.Warhead;
using CommandSystem.Commands.Shared;
using _Scripts.CommandSystem.Commands.Shared;

namespace CommandSystem
{
	public class RemoteAdminCommandHandler : CommandHandler
	{
		private RemoteAdminCommandHandler()
		{
		}

		public static RemoteAdminCommandHandler Create()
		{
			RemoteAdminCommandHandler remoteAdminCommandHandler = new RemoteAdminCommandHandler();
			remoteAdminCommandHandler.LoadGeneratedCommands();
			return remoteAdminCommandHandler;
		}

		public override void LoadGeneratedCommands()
		{
			this.RegisterCommand(new QueryCommand());
			this.RegisterCommand(new CassieClear());
			this.RegisterCommand(new AudioPoolDebug());
			this.RegisterCommand(new BuildInfoCommand());
			this.RegisterCommand(ConfigCommand.Create());
			this.RegisterCommand(new ContactCommand());
			this.RegisterCommand(new EchoCommand());
			this.RegisterCommand(new ForceStartCommand());
			this.RegisterCommand(new GlobalTagCommand());
			this.RegisterCommand(new HelloCommand());
			this.RegisterCommand(new HelpCommand(this));
			this.RegisterCommand(new HideTagCommand());
			this.RegisterCommand(new NextRoundCommand());
			this.RegisterCommand(new NoopCommand());
			this.RegisterCommand(new PermCommand());
			this.RegisterCommand(new PlayersCommand());
			this.RegisterCommand(new QuitCommand());
			this.RegisterCommand(new RedirectCommand());
			this.RegisterCommand(new RefreshCommandsCommand(this));
			this.RegisterCommand(new RestartNextRoundCommand());
			this.RegisterCommand(new RidListCommand());
			this.RegisterCommand(new ShowTagCommand());
			this.RegisterCommand(new SoftRestartCommand());
			this.RegisterCommand(new CommandSystem.Commands.Shared.SrvCfgCommand());
			this.RegisterCommand(new StopNextRoundCommand());
			this.RegisterCommand(new SubscribeCommand());
			this.RegisterCommand(new UnsubscribeCommand());
			this.RegisterCommand(new UptimeRoundsCommand());
			this.RegisterCommand(new BanCommand());
			this.RegisterCommand(new GbanKickCommand());
			this.RegisterCommand(new OfflineBanCommand());
			this.RegisterCommand(new UnbanCommand());
			this.RegisterCommand(new BringCommand());
			this.RegisterCommand(new BypassCommand());
			this.RegisterCommand(new CassieCommand());
			this.RegisterCommand(new CassieSilentCommand());
			this.RegisterCommand(new CassieWordsCommand());
			this.RegisterCommand(new ChangeColorCommand());
			this.RegisterCommand(new ChangeCustomPlayerInfoCommand());
			this.RegisterCommand(new ChangeNameCommand());
			this.RegisterCommand(new ClearEffectsCommand());
			this.RegisterCommand(new DangerCommand());
			this.RegisterCommand(new DestroyToyCommand());
			this.RegisterCommand(new DisarmCommand());
			this.RegisterCommand(new DisplayNameCommand());
			this.RegisterCommand(new EffectCommand());
			this.RegisterCommand(ElevatorCommand.Create());
			this.RegisterCommand(new ExternalLookupCommand());
			this.RegisterCommand(new ForceAttachmentsCommand());
			this.RegisterCommand(new ForceRoleCommand());
			this.RegisterCommand(new FriendlyFireDetectorCommand());
			this.RegisterCommand(new GiveLoadoutCommand());
			this.RegisterCommand(new GodCommand());
			this.RegisterCommand(new GotoCommand());
			this.RegisterCommand(new HealCommand());
			this.RegisterCommand(new AddCandyCommand());
			this.RegisterCommand(new PlayerInventoryCommand());
			this.RegisterCommand(new StripCommand());
			this.RegisterCommand(new KeyCommand());
			this.RegisterCommand(new KillCommand());
			this.RegisterCommand(new LobbyLockCommand());
			this.RegisterCommand(new IntercomResetCommand());
			this.RegisterCommand(new IntercomSpeakCommand());
			this.RegisterCommand(new IntercomTimeoutCommand());
			this.RegisterCommand(new NoclipCommand());
			this.RegisterCommand(new OverchargeCommand());
			this.RegisterCommand(new OverwatchCommand());
			this.RegisterCommand(PermissionsManagementCommand.Create());
			this.RegisterCommand(new PingCommand());
			this.RegisterCommand(new PocketDimensionDebug());
			this.RegisterCommand(new RAConfigCommand());
			this.RegisterCommand(new RconCommand());
			this.RegisterCommand(new ReleaseCommand());
			this.RegisterCommand(new ReloadConfigCommand());
			this.RegisterCommand(WaveCommand.Create());
			this.RegisterCommand(new RoomTPCommand());
			this.RegisterCommand(new RoundLockCommand());
			this.RegisterCommand(new RoundTimeCommand());
			this.RegisterCommand(new AddExperienceCommand());
			this.RegisterCommand(new SetExperienceCommand());
			this.RegisterCommand(new SetLevelCommand());
			this.RegisterCommand(new Scp3114HistoryCommand());
			this.RegisterCommand(new SetGroupCommand());
			this.RegisterCommand(new SetHealthCommand());
			this.RegisterCommand(new SpawnToyCommand());
			this.RegisterCommand(new SSSExampleCommand());
			this.RegisterCommand(new StareCommand());
			this.RegisterCommand(new StateCommand());
			this.RegisterCommand(new VersionCommand());
			this.RegisterCommand(new WikiCommand());
			this.RegisterCommand(WarheadCommand.Create());
			this.RegisterCommand(StripdownCommand.Create());
			this.RegisterCommand(ServerEventCommand.Create());
			this.RegisterCommand(new IntercomMuteCommand());
			this.RegisterCommand(new IntercomTextCommand());
			this.RegisterCommand(new IntercomUnmuteCommand());
			this.RegisterCommand(new MuteCommand());
			this.RegisterCommand(new UnmuteCommand());
			this.RegisterCommand(new ForceEquipCommand());
			this.RegisterCommand(new GiveCommand());
			this.RegisterCommand(new RemoveItemCommand());
			this.RegisterCommand(DummiesCommand.Create());
			this.RegisterCommand(new CloseDoorCommand());
			this.RegisterCommand(new DestroyDoorCommand());
			this.RegisterCommand(new DoorsListCommand());
			this.RegisterCommand(new DoorTPCommand());
			this.RegisterCommand(new LockDoorCommand());
			this.RegisterCommand(new LockdownCommand());
			this.RegisterCommand(new OpenDoorCommand());
			this.RegisterCommand(new RepairDoorCommand());
			this.RegisterCommand(new UnlockDoorCommand());
			this.RegisterCommand(DecontaminationCommand.Create());
			this.RegisterCommand(CleanupCommand.Create());
			this.RegisterCommand(new BroadcastCommand());
			this.RegisterCommand(new ClearBroadcastCommand());
			this.RegisterCommand(new PlayerBroadcastCommand());
			this.RegisterCommand(new PocketDimensionCommand());
			this.RegisterCommand(new CommandSystem.Commands.Console.RoundRestartCommand());
			this.RegisterCommand(new GiftCommand());
			this.RegisterCommand(new TeleportTreeCommand());
		}
	}
}
