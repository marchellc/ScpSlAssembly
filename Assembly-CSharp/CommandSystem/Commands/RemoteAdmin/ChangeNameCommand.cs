using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ChangeNameCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "setname";

		public string[] Aliases { get; } = new string[] { "setnickname", "setnick" };

		public string Description { get; } = "Changes or resets player's name.";

		public string[] Usage { get; } = new string[] { "%player%", "new name (optional)" };

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
				response = "Cannot find player(s) using the arguments provided!";
				return false;
			}
			string text = ((array == null) ? null : string.Join(" ", array));
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			foreach (ReferenceHub referenceHub in list)
			{
				if (text == null)
				{
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} cleared nickname of player {1} ({2}).", sender.LogName, referenceHub.PlayerId, referenceHub.nicknameSync.MyNick), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					stringBuilder.AppendFormat("Reset {0}'s display name.\n", referenceHub.LoggedNameFromRefHub());
					referenceHub.nicknameSync.DisplayName = null;
				}
				else
				{
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} set nickname of player {1} ({2}) to \"{3}\".", new object[]
					{
						sender.LogName,
						referenceHub.PlayerId,
						referenceHub.nicknameSync.MyNick,
						text
					}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					stringBuilder.AppendFormat("Set {0}'s display name to: {1}\n", referenceHub.LoggedNameFromRefHub(), text);
					referenceHub.nicknameSync.DisplayName = text;
				}
			}
			response = stringBuilder.ToString().Trim();
			StringBuilderPool.Shared.Return(stringBuilder);
			return true;
		}
	}
}
