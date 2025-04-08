using System;
using System.Collections.Generic;
using System.Linq;

namespace CommandSystem.Commands.RemoteAdmin.PermissionsManagement.Group
{
	[CommandHandler(typeof(GroupCommand))]
	public class RevokeCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "revoke";

		public string[] Aliases { get; }

		public string Description { get; } = "Revokes a permission group from a group.";

		public string[] Usage { get; } = new string[] { "Group Name", "Permission Name" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = arguments.At(0);
			string text2 = arguments.At(1);
			if (ServerStatic.PermissionsHandler.GetGroup(text) == null)
			{
				response = "Group can't be found.";
				return false;
			}
			if (!ServerStatic.PermissionsHandler.GetAllPermissions().Contains(text2))
			{
				response = "Permission can't be found.";
				return false;
			}
			Dictionary<string, string> stringDictionary = ServerStatic.RolesConfig.GetStringDictionary("Permissions");
			List<string> list = null;
			foreach (string text3 in stringDictionary.Keys)
			{
				if (!(text3 != text2))
				{
					list = YamlConfig.ParseCommaSeparatedString(stringDictionary[text3]).ToList<string>();
				}
			}
			if (list == null)
			{
				response = "Permissions can't be found in the config.";
				return false;
			}
			if (!list.Contains(text2))
			{
				response = "Group does not have that permission.";
				return false;
			}
			list.Remove(text2);
			list.Sort();
			string text4 = "[";
			foreach (string text5 in list)
			{
				if (text4 != "[")
				{
					text4 += ", ";
				}
				text4 += text5;
			}
			text4 += "]";
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[] { sender.LogName, " revoked permission ", text2, " to group ", text, "." }), ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
			ServerStatic.RolesConfig.SetStringDictionaryItem("Permissions", text2, text4);
			ServerStatic.PermissionsHandler = new PermissionsHandler(ref ServerStatic.RolesConfig, ref ServerStatic.SharedGroupsConfig, ref ServerStatic.SharedGroupsMembersConfig);
			response = "Permissions updated.";
			return true;
		}
	}
}
