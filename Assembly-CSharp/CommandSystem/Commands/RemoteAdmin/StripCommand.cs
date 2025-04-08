using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class StripCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "strip";

		public string[] Aliases { get; } = new string[] { "clear" };

		public string Description { get; } = "Clears the specified player(s) inventory.";

		public string[] Usage { get; } = new string[] { "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count >= 1)
			{
				string[] array;
				List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
				int num = 0;
				int num2 = 0;
				string text = "";
				if (list != null)
				{
					foreach (ReferenceHub referenceHub in list)
					{
						try
						{
							InventoryInfo userInventory = referenceHub.inventory.UserInventory;
							while (userInventory.Items.Count > 0)
							{
								referenceHub.inventory.ServerRemoveItem(userInventory.Items.ElementAt(0).Key, null);
							}
							userInventory.ReserveAmmo.Clear();
							referenceHub.inventory.SendAmmoNextFrame = true;
							ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " cleared " + referenceHub.LoggedNameFromRefHub() + "'s inventory.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
						}
						catch (Exception ex)
						{
							num++;
							text = ex.Message;
							continue;
						}
						num2++;
					}
				}
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} ran the STRIP command on {1} players.", sender.LogName, list.Count), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n {1}", num, text));
				return true;
			}
			response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
			return false;
		}
	}
}
