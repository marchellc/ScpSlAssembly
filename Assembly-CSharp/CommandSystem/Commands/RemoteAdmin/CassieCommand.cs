﻿using System;
using Respawning;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class CassieCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "cassie";

		public string[] Aliases { get; }

		public string Description { get; } = "Sends an announcement over the CASSIE system.";

		public string[] Usage { get; } = new string[] { "message" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Announcer, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = RAUtils.FormatArguments(arguments, 0);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " started a cassie announcement: " + text + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			RespawnEffectsController.PlayCassieAnnouncement(text, false, true, true, "");
			response = "Announcement sent!";
			return true;
		}
	}
}
