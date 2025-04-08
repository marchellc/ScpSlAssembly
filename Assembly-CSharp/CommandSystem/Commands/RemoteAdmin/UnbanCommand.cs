using System;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class UnbanCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "unban";

		public string[] Aliases { get; } = new string[] { "pardon" };

		public string Description { get; } = "Unbans the specified AuthID or IP Address.";

		public string[] Usage { get; } = new string[] { "id/ip", "AuthID/IP Address" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.LongTermBanning, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = arguments.At(0);
			if (text == "id" || text == "playerid" || text == "player")
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " unbanned player with id " + arguments.At(1) + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				BanHandler.RemoveBan(arguments.At(1), BanHandler.BanType.UserId, false);
				response = "Done!";
				return true;
			}
			if (!(text == "ip") && !(text == "address"))
			{
				response = string.Format("Incorrect syntax! Usage: unban{0}", this.Usage);
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " unbanned IP address " + arguments.At(1) + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			BanHandler.RemoveBan(arguments.At(1), BanHandler.BanType.IP, false);
			response = "Done!";
			return true;
		}
	}
}
