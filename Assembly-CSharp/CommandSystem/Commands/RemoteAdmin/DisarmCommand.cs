using System;
using System.Collections.Generic;
using System.Text;
using InventorySystem;
using InventorySystem.Disarming;
using NorthwoodLib.Pools;
using Utils;
using Utils.Networking;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class DisarmCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "disarm";

		public string[] Aliases { get; } = new string[] { "da" };

		public string Description { get; } = "Force disarming player(s), no matter if they have been cuffed before. They can be released only with RELEASE command.";

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
			if (list == null)
			{
				response = "An unexpected problem has occurred during PlayerId or name array processing.";
				return false;
			}
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (!(referenceHub == null))
				{
					DisarmingHandlers.InvokeOnPlayerDisarmed(null, referenceHub);
					referenceHub.inventory.SetDisarmedStatus(null);
					referenceHub.inventory.ServerDropEverything();
					DisarmedPlayers.Entries.Add(new DisarmedPlayers.DisarmedEntry(referenceHub.networkIdentity.netId, 0U));
					new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated(0);
					if (num != 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
					num++;
				}
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} administratively disarmed {1}.", sender.LogName, stringBuilder), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}
	}
}
