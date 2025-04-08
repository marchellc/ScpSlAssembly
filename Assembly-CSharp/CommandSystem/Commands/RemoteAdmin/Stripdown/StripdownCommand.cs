using System;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class StripdownCommand : ParentCommand, IUsageProvider
	{
		public override string Command { get; } = "stripdown";

		public override string[] Aliases
		{
			get
			{
				return null;
			}
		}

		public override string Description { get; } = "Advanced debug command. Allows to print out properties and fields of any instantiated object.";

		public string[] Usage { get; } = new string[] { "SubCommand" };

		public static StripdownCommand Create()
		{
			StripdownCommand stripdownCommand = new StripdownCommand();
			stripdownCommand.LoadGeneratedCommands();
			return stripdownCommand;
		}

		protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				return false;
			}
			response = "Please specify a valid subcommand:";
			foreach (ICommand command in this.AllCommands)
			{
				response = string.Concat(new string[] { response, "\n", command.Command, " - ", command.Description });
			}
			return false;
		}

		public override void LoadGeneratedCommands()
		{
			this.RegisterCommand(new StripdownComponentCommand());
			this.RegisterCommand(new StripdownObjectCommand());
			this.RegisterCommand(new StripdownPrintCommand());
			this.RegisterCommand(new StripdownValueCommand());
		}
	}
}
