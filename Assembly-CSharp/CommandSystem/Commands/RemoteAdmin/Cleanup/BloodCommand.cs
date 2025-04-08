using System;
using Decals;
using Utils.Networking;

namespace CommandSystem.Commands.RemoteAdmin.Cleanup
{
	[CommandHandler(typeof(CleanupCommand))]
	public class BloodCommand : ICommand
	{
		public string Command { get; } = "blood";

		public string[] Aliases { get; } = new string[] { "bl" };

		public string Description { get; } = "Cleans up blood from the map.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			string text;
			int maxValue;
			if (!arguments.Array.TryGet(2, out text) || !int.TryParse(text, out maxValue))
			{
				maxValue = int.MaxValue;
			}
			new DecalCleanupMessage(DecalPoolType.Blood, maxValue).SendToAuthenticated(0);
			response = "Cleaned up blood!";
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " has force-cleaned up blood.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			return true;
		}
	}
}
