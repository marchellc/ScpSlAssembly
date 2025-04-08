using System;
using System.Collections.Generic;
using InventorySystem.Items;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Inventory
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ForceEquipCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "forceequip";

		public string[] Aliases { get; } = new string[] { "fequip", "forceeq" };

		public string Description { get; } = "Forces player to equip item of provided type (if any exists in the inventory).";

		public string[] Usage { get; } = new string[] { "%player%", "%item%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
			{
				return false;
			}
			if (arguments.Count == 0)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			ItemType itemType = ItemType.None;
			if (array != null && array.Length != 0)
			{
				string text = array[0];
				if (text.Contains('.'))
				{
					text = text.Split('.', StringSplitOptions.None)[0];
				}
				int num;
				if (int.TryParse(text, out num) && Enum.IsDefined(typeof(ItemType), num))
				{
					itemType = (ItemType)num;
				}
			}
			int num2 = 0;
			int num3 = 0;
			string empty = string.Empty;
			if (list != null)
			{
				Func<ReferenceHub, ItemType, bool> func = ((itemType == ItemType.None) ? new Func<ReferenceHub, ItemType, bool>(this.TryHolster) : new Func<ReferenceHub, ItemType, bool>(this.TryEquip));
				foreach (ReferenceHub referenceHub in list)
				{
					if (func(referenceHub, itemType))
					{
						num3++;
					}
				}
			}
			response = ((num2 == 0) ? string.Format("Done! The request affected {0} player{1}", num3, (num3 == 1) ? "!" : "s!") : string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num2, empty));
			return true;
		}

		private bool TryEquip(ReferenceHub hub, ItemType itemToEquip)
		{
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in hub.inventory.UserInventory.Items)
			{
				if (keyValuePair.Value.ItemTypeId == itemToEquip)
				{
					hub.inventory.ServerSelectItem(keyValuePair.Key);
					return true;
				}
			}
			return false;
		}

		private bool TryHolster(ReferenceHub hub, ItemType parsedItem)
		{
			if (hub.inventory.CurItem.SerialNumber == 0)
			{
				return false;
			}
			hub.inventory.ServerSelectItem(0);
			return false;
		}
	}
}
