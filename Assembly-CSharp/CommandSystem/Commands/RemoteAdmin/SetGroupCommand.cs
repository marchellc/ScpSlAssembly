using System;
using System.Collections.Generic;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class SetGroupCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "setgroup";

		public string[] Aliases { get; } = new string[] { "sg", "setrole" };

		public string Description { get; } = "Temporarily assigns the specified player(s) to a specified group.";

		public string[] Usage { get; } = new string[] { "%player%", "Group (-1 Removes)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.SetGroup, out response))
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
			UserGroup userGroup = null;
			if (array[0] != "-1")
			{
				userGroup = ServerStatic.PermissionsHandler.GetGroup(array[0]);
				if (userGroup == null)
				{
					response = "Requested group doesn't exist! Use group \"-1\" to remove user group.";
					return false;
				}
			}
			int num = 0;
			int num2 = 0;
			string text = string.Empty;
			foreach (ReferenceHub referenceHub in list)
			{
				if (referenceHub.encryptedChannelManager.EncryptionKey == null)
				{
					text = text + "Empty encryption key of player " + referenceHub.nicknameSync.MyNick + ". Make sure to use online mode or ask that player to use RA password before changing the role.\n";
					num2++;
				}
				else
				{
					if (userGroup == null)
					{
						ServerLogs.AddLog(ServerLogs.Modules.Permissions, sender.LogName + " removed local group from player " + referenceHub.LoggedNameFromRefHub() + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					}
					else
					{
						ServerLogs.AddLog(ServerLogs.Modules.Permissions, string.Concat(new string[]
						{
							sender.LogName,
							" set local group of player ",
							referenceHub.LoggedNameFromRefHub(),
							" to ",
							array[0],
							"."
						}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					}
					referenceHub.serverRoles.SetGroup(userGroup, true, false);
					num++;
				}
			}
			if (num2 == 0)
			{
				response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
				return true;
			}
			response = string.Format("Failed to execute the command! Failures: {0}\nError log:\n{1}", num2, text);
			return false;
		}
	}
}
