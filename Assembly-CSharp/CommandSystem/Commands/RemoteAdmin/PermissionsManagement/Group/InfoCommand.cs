﻿using System;

namespace CommandSystem.Commands.RemoteAdmin.PermissionsManagement.Group
{
	[CommandHandler(typeof(GroupCommand))]
	public class InfoCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "info";

		public string[] Aliases { get; }

		public string Description { get; } = "Displays group info.";

		public string[] Usage { get; } = new string[] { "Group Name" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			UserGroup group = ServerStatic.PermissionsHandler.GetGroup(arguments.At(0));
			if (group == null)
			{
				response = "Group can't be found.";
				return false;
			}
			response = string.Format("Details of group {0}\nTag text: {1}\nTag color: {2}\nPermissions: {3}\nCover: {4}\nHidden by default: {5}\nKick power: {6}\nRequired kick power: {7}", new object[]
			{
				arguments.At(0),
				group.BadgeText,
				group.BadgeColor,
				group.Permissions,
				group.Cover ? "YES" : "NO",
				group.HiddenByDefault ? "YES" : "NO",
				group.KickPower,
				group.RequiredKickPower
			});
			return true;
		}
	}
}
