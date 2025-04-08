using System;
using System.Collections.Generic;
using System.Text;
using PlayerRoles;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class RoleListCommand : ICommand
	{
		public string Command { get; } = "rolelist";

		public string[] Aliases { get; }

		public string Description { get; } = "Display a list of roles.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<size=25>List of role:</size>");
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
			{
				PlayerRoleBase value = keyValuePair.Value;
				IHiddenRole hiddenRole = value as IHiddenRole;
				if (hiddenRole == null || !hiddenRole.IsHidden)
				{
					stringBuilder.AppendLine(string.Format("Role #{0:000} : {1} - \"{2}\"", (int)keyValuePair.Key, keyValuePair.Key, value.RoleName));
				}
			}
			response = stringBuilder.ToString();
			return true;
		}
	}
}
