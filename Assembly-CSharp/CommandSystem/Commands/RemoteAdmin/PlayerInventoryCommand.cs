using System;
using System.Collections.Generic;
using System.Text;
using InventorySystem.Items;
using NorthwoodLib.Pools;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class PlayerInventoryCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "playerinventory";

		public string[] Aliases { get; } = new string[] { "playerinv", "pinv", "pinventory" };

		public string Description { get; } = "Displays players inventory.";

		public string[] Usage { get; } = new string[] { "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (list == null || list.Count == 0)
			{
				response = "Cannot find player with id or name: " + arguments.At(0);
				return false;
			}
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			foreach (ReferenceHub referenceHub in list)
			{
				string text = referenceHub.LoggedNameFromRefHub();
				ServerLogs.AddLog(ServerLogs.Modules.DataAccess, sender.LogName + " displayed the inventory of player " + text + ".", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
				stringBuilder.AppendFormat("Inventory of {0}:", text);
				foreach (KeyValuePair<ushort, ItemBase> keyValuePair in referenceHub.inventory.UserInventory.Items)
				{
					stringBuilder.AppendFormat("\n - {0}", keyValuePair.Value.ItemTypeId);
				}
				stringBuilder.AppendLine();
			}
			response = stringBuilder.ToString().Trim();
			StringBuilderPool.Shared.Return(stringBuilder);
			return true;
		}
	}
}
