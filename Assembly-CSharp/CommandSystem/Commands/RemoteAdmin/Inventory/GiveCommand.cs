using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Inventory
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class GiveCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "give";

		public string[] Aliases { get; }

		public string Description { get; } = "Give player(s) a specified item.";

		public string[] Usage { get; } = new string[] { "%player%", "%item%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (array == null || array.Length == 0)
			{
				response = "You must specify item(s) to give.";
				return false;
			}
			ItemType[] array2 = GiveCommand.ParseItems(array[0]).ToArray<ItemType>();
			if (array2.Length == 0)
			{
				response = "You didn't input any items.";
				return false;
			}
			int num = 0;
			int num2 = 0;
			string text = string.Empty;
			if (list != null)
			{
				foreach (ReferenceHub referenceHub in list)
				{
					try
					{
						foreach (ItemType itemType in array2)
						{
							GiveCommand.AddItem(referenceHub, sender, itemType);
						}
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
			response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num, text));
			return true;
		}

		private static IEnumerable<ItemType> ParseItems(string argument)
		{
			string[] array = argument.Split('.', StringSplitOptions.None);
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				int num;
				if (int.TryParse(array2[i], out num) && Enum.IsDefined(typeof(ItemType), num))
				{
					yield return (ItemType)num;
				}
			}
			array2 = null;
			yield break;
		}

		private static void AddItem(ReferenceHub ply, ICommandSender sender, ItemType id)
		{
			global::UnityEngine.Object @object = ply.inventory.ServerAddItem(id, ItemAddReason.AdminCommand, 0, null);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} gave {1} to {2}.", sender.LogName, id, ply.LoggedNameFromRefHub()), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			if (@object == null)
			{
				throw new NullReferenceException(string.Format("Could not add {0}. Inventory is full or the item is not defined.", id));
			}
		}
	}
}
