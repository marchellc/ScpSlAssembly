using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;

namespace CommandSystem.Commands.RemoteAdmin.PermissionsManagement
{
	[CommandHandler(typeof(PermissionsManagementCommand))]
	public class UsersCommand : ICommand
	{
		public string Command { get; } = "users";

		public string[] Aliases { get; }

		public string Description { get; } = "List all the users that are assigned to any group.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				return false;
			}
			Dictionary<string, string> stringDictionary = ServerStatic.RolesConfig.GetStringDictionary("Members");
			YamlConfig sharedGroupsMembersConfig = ServerStatic.SharedGroupsMembersConfig;
			Dictionary<string, string> dictionary = ((sharedGroupsMembersConfig != null) ? sharedGroupsMembersConfig.GetStringDictionary("SharedMembers") : null);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent("Players with assigned groups:");
			foreach (KeyValuePair<string, string> keyValuePair in stringDictionary)
			{
				stringBuilder.Append("\n" + keyValuePair.Key + " - " + keyValuePair.Value);
			}
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, string> keyValuePair2 in dictionary)
				{
					stringBuilder.Append(string.Concat(new string[] { "\n", keyValuePair2.Key, " - ", keyValuePair2.Value, " <color=#FFD700>[SHARED MEMBERSHIP]</color>" }));
				}
			}
			response = stringBuilder.ToString();
			StringBuilderPool.Shared.Return(stringBuilder);
			return true;
		}
	}
}
