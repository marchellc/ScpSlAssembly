using System;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class WaveCommand : ParentCommand, IUsageProvider
	{
		public override string Command { get; } = "wave";

		public override string[] Aliases { get; } = new string[] { "wv" };

		public override string Description { get; } = "Controls or displays information about the Wave system.";

		public string[] Usage { get; } = new string[] { "set/get/list/spawn", "<wave>", "[type] [value]" };

		public static WaveCommand Create()
		{
			WaveCommand waveCommand = new WaveCommand();
			waveCommand.LoadGeneratedCommands();
			return waveCommand;
		}

		protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = string.Concat(new string[]
			{
				"Unknown subcommand.\nUsage: ",
				this.Command,
				" ",
				this.DisplayCommandUsage(),
				"."
			});
			return false;
		}

		public override void LoadGeneratedCommands()
		{
			this.RegisterCommand(new WaveDebugCommand());
			this.RegisterCommand(new WaveListCommand());
			this.RegisterCommand(new WaveSetCommand());
			this.RegisterCommand(new WaveSpawnCommand());
		}
	}
}
