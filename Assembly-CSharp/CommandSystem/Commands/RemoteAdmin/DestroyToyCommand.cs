using System;
using AdminToys;
using Mirror;
using Utils.Networking;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class DestroyToyCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "destroytoy";

		public string[] Aliases { get; }

		public string Description { get; } = "Despawns a toy placed by an admin.";

		public string[] Usage { get; } = new string[] { "NetID of toy to remove." };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			uint num;
			if (arguments.Count < 1 || !uint.TryParse(arguments.Array[1], out num))
			{
				response = "Failed to parse NetID of the toy to destroy.";
				return false;
			}
			NetworkIdentity networkIdentity;
			AdminToyBase adminToyBase;
			if (!NetworkUtils.SpawnedNetIds.TryGetValue(num, out networkIdentity) || !networkIdentity.TryGetComponent<AdminToyBase>(out adminToyBase))
			{
				response = string.Format("{0} is not a valid toy NetID.", num);
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} removed admin toy: {1} ({2}).", sender.LogName, adminToyBase.CommandName, adminToyBase.netId), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = string.Format("Toy {0} successfully removed.", num);
			NetworkServer.Destroy(adminToyBase.gameObject);
			return true;
		}
	}
}
