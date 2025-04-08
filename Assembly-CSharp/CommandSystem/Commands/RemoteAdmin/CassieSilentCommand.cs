using System;
using Respawning;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class CassieSilentCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "cassie_silent";

		public string[] Aliases { get; } = new string[] { "cassie_silentnoise", "cassie_sn", "cassie_sl" };

		public string Description { get; } = "Sends a silent (no preceding tone) announcement over the CASSIE system.";

		public string[] Usage { get; } = new string[] { "message" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Announcer, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = RAUtils.FormatArguments(arguments, 0);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " started a silent cassie announcement: " + text + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			RespawnEffectsController.PlayCassieAnnouncement(text, false, false, true, "");
			response = "Silent announcement sent!";
			return true;
		}
	}
}
