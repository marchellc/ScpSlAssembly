using System;
using Interactables.Interobjects;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ElevatorCommand : ParentCommand, IUsageProvider
	{
		public override string Command { get; } = "elevator";

		public override string[] Aliases { get; } = new string[] { "lift", "elev", "el", "lt" };

		public override string Description { get; } = "Allows to check status of all elevators and force an elevator to move to a specific floor.";

		public string[] Usage { get; } = new string[] { "list/lock/send/teleport/unlock", "Elevator ID / \"all\"" };

		public static ElevatorCommand Create()
		{
			ElevatorCommand elevatorCommand = new ElevatorCommand();
			elevatorCommand.LoadGeneratedCommands();
			return elevatorCommand;
		}

		protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = "Please specify a valid subcommand!\n- elevator list [\"detailed\"]\n- elevator lock <Elevator ID / \"all\"> <level / \"dynamic\" (optional)>\n- elevator send <Elevator ID / \"all\"> [level ID]\n- elevator teleport <Elevator ID> <Target Players> [\"inside\"/level ID/\"outside\"]\n- elevator unlock <Elevator ID / \"all\"> <level (optional)>\n";
			return false;
		}

		public override void LoadGeneratedCommands()
		{
			this.RegisterCommand(new ElevatorListCommand());
			this.RegisterCommand(new ElevatorLockCommand());
			this.RegisterCommand(new ElevatorSendCommand());
			this.RegisterCommand(new ElevatorTeleportCommand());
			this.RegisterCommand(new ElevatorUnlockCommand());
		}

		internal static bool TryParseGroup(string txt, out ElevatorGroup group)
		{
			int num;
			if (!int.TryParse(txt, out num))
			{
				return Enum.TryParse<ElevatorGroup>(txt, true, out group);
			}
			group = (ElevatorGroup)num;
			return Enum.IsDefined(typeof(ElevatorGroup), group);
		}
	}
}
