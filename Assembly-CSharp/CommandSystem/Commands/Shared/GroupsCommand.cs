using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem.Commands.RemoteAdmin;
using NorthwoodLib.Pools;
using RemoteAdmin;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(ClientCommandHandler))]
	[CommandHandler(typeof(PermissionsManagementCommand))]
	public class GroupsCommand : ICommand
	{
		public string Command { get; } = "groups";

		public string[] Aliases { get; }

		public string Description { get; } = "Lists all defined server groups.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender != null && !playerCommandSender.ReferenceHub.authManager.BypassBansFlagSet && !playerCommandSender.ReferenceHub.isLocalPlayer && !sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				response = "You don't have permissions to execute this command.";
				return false;
			}
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent("Groups defined on this server:");
			try
			{
				Dictionary<string, UserGroup> allGroups = ServerStatic.PermissionsHandler.GetAllGroups();
				ServerRoles.NamedColor[] namedColors = ReferenceHub.LocalHub.serverRoles.NamedColors;
				foreach (KeyValuePair<string, UserGroup> keyValuePair in allGroups)
				{
					string key = keyValuePair.Key;
					UserGroup group = keyValuePair.Value;
					try
					{
						ServerRoles.NamedColor namedColor = namedColors.FirstOrDefault((ServerRoles.NamedColor x) => x.Name == group.BadgeColor);
						string text = ((namedColor != null) ? namedColor.ColorHex : null);
						stringBuilder.AppendFormat("\n{0} ({1}) - <color=#{2}>{3}</color> in color {4}", new object[] { key, group.Permissions, text, group.BadgeText, group.BadgeColor });
					}
					catch
					{
						stringBuilder.AppendFormat("\n{0} ({1}) - {2} in color {3}", new object[] { key, group.Permissions, group.BadgeText, group.BadgeColor });
					}
					foreach (KeyValuePair<PlayerPermissions, string> keyValuePair2 in PermissionsHandler.PermissionCodes)
					{
						if (PermissionsHandler.IsPermitted(keyValuePair.Value.Permissions, keyValuePair2.Key))
						{
							stringBuilder.AppendFormat(" {0}", keyValuePair2.Value);
						}
					}
				}
				response = stringBuilder.ToString();
			}
			finally
			{
				StringBuilderPool.Shared.Return(stringBuilder);
			}
			return true;
		}
	}
}
