﻿using System;
using GameCore;

namespace CommandSystem.Commands.RemoteAdmin.Dummies
{
	[CommandHandler(typeof(DummiesCommand))]
	public class SpawnDummyCommand : ICommand
	{
		public string Command { get; } = "spawn";

		public string[] Aliases { get; } = new string[] { "s", "create" };

		public string Description { get; } = "Spawns a dummy on the map.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			string text = ((arguments.Count > 0) ? string.Join<string>(' ', arguments) : "Dummy");
			DummyUtils.SpawnDummy(text);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " has spawned a dummy with the nickname " + text + ".", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
			response = "A dummy has been spawned.";
			return true;
		}
	}
}
