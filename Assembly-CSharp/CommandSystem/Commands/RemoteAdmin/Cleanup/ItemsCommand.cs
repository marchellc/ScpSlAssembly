using System;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Cleanup
{
	[CommandHandler(typeof(CleanupCommand))]
	public class ItemsCommand : ICommand
	{
		public string Command { get; } = "items";

		public string[] Aliases { get; } = new string[] { "item", "i", "1" };

		public string Description { get; } = "Cleans up items from the map.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			ItemPickupBase[] array = global::UnityEngine.Object.FindObjectsOfType<ItemPickupBase>();
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
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} has force-cleaned up {1} items.", sender.LogName, num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = string.Format("{0} items have been deleted.", num);
			return true;
		}
	}
}
