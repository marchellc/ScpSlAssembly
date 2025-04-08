using System;
using System.Collections.Generic;
using CommandSystem;
using Utils;

namespace Christmas.Scp2536
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class GiftCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "gift";

		public string[] Aliases { get; } = new string[] { "addgift" };

		public string Description { get; } = "Grant a gift to a player.";

		public string[] Usage { get; } = new string[] { "%player%", "Gift name (Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
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
			if (list == null)
			{
				response = "Cannot find player! Try using the player ID!";
				return false;
			}
			Scp2536GiftBase scp2536GiftBase = null;
			bool flag = arguments.Count > 0 && this.TryFetchGift(array[0].ToUpper(), out scp2536GiftBase);
			foreach (ReferenceHub referenceHub in list)
			{
				if (flag)
				{
					scp2536GiftBase.ServerGrant(referenceHub);
				}
				else
				{
					Scp2536Controller.Singleton.GiftController.ServerGrantRandomGift(referenceHub);
				}
			}
			return true;
		}

		private bool TryFetchGift(string input, out Scp2536GiftBase giftBase)
		{
			foreach (Scp2536GiftBase scp2536GiftBase in Scp2536GiftController.Gifts)
			{
				if (scp2536GiftBase.GetType().Name.ToUpper().Contains(input))
				{
					giftBase = scp2536GiftBase;
					return true;
				}
			}
			giftBase = null;
			return false;
		}
	}
}
