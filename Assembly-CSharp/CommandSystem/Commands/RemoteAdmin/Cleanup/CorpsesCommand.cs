using System;
using System.Linq;
using Mirror;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Cleanup
{
	[CommandHandler(typeof(CleanupCommand))]
	public class CorpsesCommand : ICommand
	{
		public string Command { get; } = "corpses";

		public string[] Aliases { get; } = new string[] { "corpse", "ragdolls", "ragdoll", "r", "c", "0" };

		public string Description { get; } = "Cleans up ragdolls from the map.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			BasicRagdoll[] array = (from r in global::UnityEngine.Object.FindObjectsOfType<BasicRagdoll>()
				orderby r.Info.CreationTime descending
				select r).ToArray<BasicRagdoll>();
			int num = array.Length;
			int num2;
			if (arguments.Count > 0 && int.TryParse(arguments.At(0), out num2) && num2 < array.Length)
			{
				num = num2;
			}
			for (int i = 0; i < num; i++)
			{
				NetworkServer.Destroy(array[i].gameObject);
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} has force-cleaned up {1} ragdolls.", sender.LogName, num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = string.Format("{0} ragdolls have been deleted.", num);
			return true;
		}
	}
}
