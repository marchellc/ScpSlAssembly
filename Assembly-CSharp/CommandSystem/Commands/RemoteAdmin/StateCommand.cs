using System;
using PlayerRoles.PlayableScps.Scp096;
using RemoteAdmin;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class StateCommand : ICommand
	{
		public string Command { get; } = "096state";

		public string[] Aliases { get; } = new string[] { "state096", "state" };

		public string Description { get; } = "Prints which state you are in server-side as 096.";

		public string[] Usage { get; } = new string[] { "a" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(new PlayerPermissions[]
			{
				PlayerPermissions.ForceclassSelf,
				PlayerPermissions.ForceclassWithoutRestrictions
			}, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "Only players can run this command.";
				return false;
			}
			Scp096Role scp096Role = playerCommandSender.ReferenceHub.roleManager.CurrentRole as Scp096Role;
			if (scp096Role == null)
			{
				response = "You must be SCP-096 to use this command.";
				return false;
			}
			response = string.Format("Your current state server-side is: RageState: {0} | AbilityState: {1}", scp096Role.StateController.RageState, scp096Role.StateController.AbilityState);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + "'s " + response, ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			return true;
		}
	}
}
