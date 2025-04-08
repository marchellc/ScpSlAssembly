using System;

namespace CommandSystem.Commands.RemoteAdmin.PermissionsManagement.Group
{
	[CommandHandler(typeof(PermissionsManagementCommand))]
	public class GroupCommand : ParentCommand
	{
		public override string Command { get; } = "group";

		public override string[] Aliases { get; } = new string[] { "gr" };

		public override string Description { get; } = "Group management";

		public static GroupCommand Create()
		{
			GroupCommand groupCommand = new GroupCommand();
			groupCommand.LoadGeneratedCommands();
			return groupCommand;
		}

		protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = "Invalid subcommand. Available commands: info, grant, setcolor, settag, enablecover.";
			return true;
		}

		public override void LoadGeneratedCommands()
		{
			this.RegisterCommand(new DisableCoverCommand());
			this.RegisterCommand(new EnableCoverCommand());
			this.RegisterCommand(new GrantCommand());
			this.RegisterCommand(new InfoCommand());
			this.RegisterCommand(new RevokeCommand());
			this.RegisterCommand(new SetColorCommand());
			this.RegisterCommand(new SetTagCommand());
		}
	}
}
