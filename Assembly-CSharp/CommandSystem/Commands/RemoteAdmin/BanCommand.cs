using System;
using System.Collections.Generic;
using System.Linq;
using GameCore;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class BanCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "ban";

		public string[] Aliases { get; }

		public string Description { get; } = "Bans a player.";

		public string[] Usage { get; } = new string[] { "%player%", "Duration", "Reason" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (arguments.Count < 3)
			{
				response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (list == null)
			{
				response = "An unexpected problem has occurred during PlayerId/Name array processing.";
				return false;
			}
			if (array == null)
			{
				response = "An error occured while processing this command.\nUsage: " + this.DisplayCommandUsage();
				return false;
			}
			string text = string.Empty;
			if (array.Length > 1)
			{
				text = array.Skip(1).Aggregate((string current, string n) => current + " " + n);
			}
			long num;
			try
			{
				num = Misc.RelativeTimeToSeconds(array[0], 60);
			}
			catch
			{
				response = "Invalid time: " + array[0];
				return false;
			}
			if (num < 0L)
			{
				num = 0L;
				array[0] = "0";
			}
			if (num == 0L && !sender.CheckPermission(new PlayerPermissions[]
			{
				PlayerPermissions.KickingAndShortTermBanning,
				PlayerPermissions.BanningUpToDay,
				PlayerPermissions.LongTermBanning
			}, out response))
			{
				return false;
			}
			if (num > 0L && num <= 3600L && !sender.CheckPermission(PlayerPermissions.KickingAndShortTermBanning, out response))
			{
				return false;
			}
			if (num > 3600L && num <= 86400L && !sender.CheckPermission(PlayerPermissions.BanningUpToDay, out response))
			{
				return false;
			}
			if (num > 86400L && !sender.CheckPermission(PlayerPermissions.LongTermBanning, out response))
			{
				return false;
			}
			ushort num2 = 0;
			ushort num3 = 0;
			string text2 = string.Empty;
			foreach (ReferenceHub referenceHub in list)
			{
				try
				{
					if (referenceHub == null)
					{
						num3 += 1;
					}
					else
					{
						string combinedName = referenceHub.nicknameSync.CombinedName;
						CommandSender commandSender = sender as CommandSender;
						if (commandSender != null && !commandSender.FullPermissions)
						{
							UserGroup group = referenceHub.serverRoles.Group;
							byte b = ((group != null) ? group.RequiredKickPower : 0);
							if (b > commandSender.KickPower)
							{
								num3 += 1;
								text2 = string.Format("You can't kick/ban {0}. Required kick power: {1}, your kick power: {2}.", combinedName, b, commandSender.KickPower);
								sender.Respond(text2, false);
								continue;
							}
						}
						ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[]
						{
							sender.LogName,
							" banned player ",
							referenceHub.LoggedNameFromRefHub(),
							". Ban duration: ",
							array[0],
							". Reason: ",
							(text == string.Empty) ? "(none)" : text,
							"."
						}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
						if (CustomNetworkManager.IsVerified && referenceHub.authManager.BypassBansFlagSet)
						{
							BanPlayer.KickUser(referenceHub, sender, text);
						}
						else
						{
							if (num == 0L && ConfigFile.ServerConfig.GetBool("broadcast_kicks", false))
							{
								Broadcast.Singleton.RpcAddElement(ConfigFile.ServerConfig.GetString("broadcast_kick_text", "%nick% has been kicked from this server.").Replace("%nick%", combinedName), ConfigFile.ServerConfig.GetUShort("broadcast_kick_duration", 5), Broadcast.BroadcastFlags.Normal);
							}
							else if (num != 0L && ConfigFile.ServerConfig.GetBool("broadcast_bans", true))
							{
								Broadcast.Singleton.RpcAddElement(ConfigFile.ServerConfig.GetString("broadcast_ban_text", "%nick% has been banned from this server.").Replace("%nick%", combinedName), ConfigFile.ServerConfig.GetUShort("broadcast_ban_duration", 5), Broadcast.BroadcastFlags.Normal);
							}
							BanPlayer.BanUser(referenceHub, sender, text, num);
						}
						num2 += 1;
					}
				}
				catch (Exception ex)
				{
					num3 += 1;
					Debug.Log(ex);
					text2 = "Error occured during banning: " + ex.Message + ".\n" + ex.StackTrace;
				}
			}
			if (num3 == 0)
			{
				string text3 = "Banned";
				int num4;
				if (int.TryParse(array[0], out num4))
				{
					text3 = ((num4 > 0) ? "Banned" : "Kicked");
				}
				response = string.Format("Done! {0} {1} player{2}", text3, num2, (num2 == 1) ? "!" : "s!");
				return true;
			}
			response = string.Format("Failed to execute the command! Failures: {0}\nLast error log:\n{1}", num3, text2);
			return false;
		}
	}
}
