﻿using System;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class RconCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "remoteconsole";

		public string[] Aliases { get; } = new string[] { "remoteconsole", "rcon", "sudo" };

		public string Description { get; } = "Runs a command as the server console.";

		public string[] Usage { get; } = new string[] { "Command" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			if (arguments.At(0).StartsWith("!") && !ServerStatic.RolesConfig.GetBool("allow_central_server_commands_as_ServerConsoleCommands", false))
			{
				response = "Running central server commands in Remote Admin is disabled in RA config file!";
				return false;
			}
			CommandSender commandSender = sender as CommandSender;
			if (commandSender == null)
			{
				response = "You must be a CommandSender to execute this command!";
				return false;
			}
			string text = RAUtils.FormatArguments(arguments, 0);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " executed command as server console: " + text + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			ServerConsole.EnterCommand(text, commandSender);
			response = "Command \"" + text + "\" executed in server console!";
			return true;
		}
	}
}
