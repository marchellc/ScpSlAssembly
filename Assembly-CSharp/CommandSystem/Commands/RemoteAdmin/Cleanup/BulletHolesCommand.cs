using System;
using Decals;
using Utils.Networking;

namespace CommandSystem.Commands.RemoteAdmin.Cleanup
{
	[CommandHandler(typeof(CleanupCommand))]
	public class BulletHolesCommand : ICommand
	{
		public string Command { get; } = "bulletholes";

		public string[] Aliases { get; } = new string[] { "bh", "bullets" };

		public string Description { get; } = "Cleans up bulled holes from the map.";

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
			foreach (DecalPoolType decalPoolType in EnumUtils<DecalPoolType>.Values)
			{
				if (decalPoolType != DecalPoolType.None && decalPoolType != DecalPoolType.Blood)
				{
					new DecalCleanupMessage(decalPoolType, maxValue).SendToAuthenticated(0);
				}
			}
			response = "Cleaned up bullet holes!";
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " has force-cleaned up bullet holes.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			return true;
		}
	}
}
