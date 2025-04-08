using System;
using System.Text;
using Mirror;
using NorthwoodLib.Pools;
using RemoteAdmin;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class PermCommand : ICommand
	{
		public string Command { get; } = "perm";

		public string[] Aliases { get; }

		public string Description { get; } = "Lists your permissions.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			CommandSender commandSender = sender as CommandSender;
			if (commandSender == null)
			{
				response = "You can't use this command (you don't have any permissions set)";
				return false;
			}
			if (!NetworkServer.active)
			{
				response = "This command can only be used on a server.";
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			string text;
			if (playerCommandSender != null)
			{
				text = ((playerCommandSender.ReferenceHub.serverRoles.Group == null) ? "0" : playerCommandSender.ReferenceHub.serverRoles.Group.RequiredKickPower.ToString());
			}
			else
			{
				text = "N/A";
			}
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			bool flag;
			try
			{
				ulong permissions = commandSender.Permissions;
				stringBuilder.AppendFormat("Your kick power: {0}\nKick power required to kick you: {1}\nYour permissions:", commandSender.KickPower, text);
				foreach (string text2 in ServerStatic.PermissionsHandler.GetAllPermissions())
				{
					string text3 = (ServerStatic.PermissionsHandler.IsRaPermitted(ServerStatic.PermissionsHandler.GetPermissionValue(text2)) ? "*" : "");
					stringBuilder.AppendFormat("\n{0}{1} ({2}): {3}", new object[]
					{
						text2,
						text3,
						ServerStatic.PermissionsHandler.GetPermissionValue(text2),
						ServerStatic.PermissionsHandler.IsPermitted(permissions, text2) ? "YES" : "NO"
					});
				}
				response = stringBuilder.ToString();
				flag = true;
			}
			finally
			{
				StringBuilderPool.Shared.Return(stringBuilder);
			}
			return flag;
		}
	}
}
