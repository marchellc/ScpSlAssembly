using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp330;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class AddCandyCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "addcandy";

		public string[] Aliases { get; } = new string[] { "candy", "trickortreat" };

		public string Description { get; } = "Adds specific candies to player(s) bags. If a player does not have a bag, it will create a new one with a random candy.";

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
				response = "Could not process provided arguments.";
				return false;
			}
			string text = (array[0].ToUpper()[0].ToString() + array[0].ToLower()).Remove(1, 1);
			CandyKindID candyKindID;
			if (!Enum.TryParse<CandyKindID>(text, out candyKindID))
			{
				response = array[0] + " could not be parsed as candy type";
				return false;
			}
			if (!Enum.IsDefined(typeof(CandyKindID), candyKindID) || candyKindID == CandyKindID.None)
			{
				response = text + " isn't a valid candy type!";
				return false;
			}
			int num = 0;
			int num2 = 0;
			string text2 = string.Empty;
			if (list != null)
			{
				foreach (ReferenceHub referenceHub in list)
				{
					try
					{
						ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} gave {1} candy to {2}.", sender.LogName, candyKindID, referenceHub.LoggedNameFromRefHub()), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
						referenceHub.GrantCandy(candyKindID, ItemAddReason.AdminCommand);
						num2++;
					}
					catch (Exception ex)
					{
						num++;
						text2 = ex.Message;
					}
				}
			}
			response = ((num == 0) ? string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!") : string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num, text2));
			return true;
		}
	}
}
