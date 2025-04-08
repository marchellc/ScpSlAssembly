using System;
using System.Collections.Generic;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class FriendlyFireDetectorCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "friendlyfiredetector";

		public string[] Aliases { get; } = new string[] { "tk", "tkd", "teamkilldetector", "ffd" };

		public string Description { get; } = "Friendly fire detection and logging";

		public string[] Usage { get; } = new string[] { "Player ID or Name/status/pause/unpause>" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			string text = arguments.At(0);
			if (text == "status")
			{
				response = "FFD#Friendly fire detector is currently " + (FriendlyFireConfig.PauseDetector ? string.Empty : "**NOT** ") + "paused.";
				return true;
			}
			if (!(text == "pause"))
			{
				if (!(text == "unpause"))
				{
					string[] array;
					List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
					if (list != null && list.Count != 1)
					{
						response = "FFD command requires exact one selected player, not an array of players!";
						return false;
					}
					if (list == null)
					{
						response = "The specified player was not found! \nUsage: FFD <Player ID or Name/status/pause/unpause>";
						return false;
					}
					FriendlyFireHandler friendlyFireHandler = list[0].FriendlyFireHandler;
					response = string.Format("--- Friendly Fire Detector Stats ---\nKills - Damage dealt\n\nRound: {0} - {1}\nLife: {2} - {3}\nWindow: {4} - {5} [Window: {6}s]\nRespawn: {7} - {8} [Window: {9}s]", new object[]
					{
						friendlyFireHandler.Round.Kills,
						friendlyFireHandler.Round.Damage,
						friendlyFireHandler.Life.Kills,
						friendlyFireHandler.Life.Damage,
						friendlyFireHandler.Window.Kills,
						friendlyFireHandler.Window.Damage,
						FriendlyFireConfig.Window,
						friendlyFireHandler.Respawn.Kills,
						friendlyFireHandler.Respawn.Damage,
						FriendlyFireConfig.RespawnWindow
					});
					return true;
				}
				else
				{
					if (!FriendlyFireConfig.PauseDetector)
					{
						response = "Friendly fire detector is not paused.";
						return false;
					}
					FriendlyFireConfig.PauseDetector = false;
					response = "Friendly fire detector has been unpaused.";
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " unpaused paused Friendly Fire Detector.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					return true;
				}
			}
			else
			{
				if (FriendlyFireConfig.PauseDetector)
				{
					response = "Friendly fire detector is already paused.";
					return false;
				}
				FriendlyFireConfig.PauseDetector = true;
				response = "Friendly fire detector has been paused.";
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " paused Friendly Fire Detector.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				return true;
			}
		}
	}
}
