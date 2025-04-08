using System;
using PlayerRoles.Voice;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class IntercomResetCommand : ICommand
	{
		public string Command { get; } = "intercom-reset";

		public string[] Aliases { get; } = new string[] { "icomreset", "ir" };

		public string Description { get; } = "Resets the timer on the intercom to ready-to-speak.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(new PlayerPermissions[]
			{
				PlayerPermissions.RoundEvents,
				PlayerPermissions.FacilityManagement,
				PlayerPermissions.PlayersManagement
			}, out response))
			{
				return false;
			}
			if (Intercom.State != IntercomState.Cooldown)
			{
				response = "Intercom is not currently on cooldown. Current state: " + Intercom.State.ToString();
				return false;
			}
			Intercom.State = IntercomState.Ready;
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " reset the intercom cooldown.", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
			response = "Done! Intercom cooldown reset.";
			return true;
		}
	}
}
