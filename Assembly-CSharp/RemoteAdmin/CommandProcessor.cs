using System;
using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using CommandSystem;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using PlayerStatsSystem;
using RemoteAdmin.Communication;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace RemoteAdmin
{
	public static class CommandProcessor
	{
		internal static void ProcessAdminChat(string q, CommandSender sender)
		{
			if (!CommandProcessor.CheckPermissions(sender, "Admin Chat", PlayerPermissions.AdminChat, string.Empty, true))
			{
				PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
				if (playerCommandSender != null)
				{
					playerCommandSender.ReferenceHub.gameConsoleTransmission.SendToClient("You don't have permissions to access Admin Chat!", "red");
					playerCommandSender.RaReply("You don't have permissions to access Admin Chat!", false, true, "");
				}
				return;
			}
			uint num = 0U;
			PlayerCommandSender playerCommandSender2 = sender as PlayerCommandSender;
			ReferenceHub referenceHub;
			if (playerCommandSender2 != null)
			{
				num = playerCommandSender2.ReferenceHub.netId;
			}
			else if (ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				num = referenceHub.netId;
			}
			q = Misc.SanitizeRichText(q.Replace("~", "-"), "[", "]").Trim();
			if (string.IsNullOrWhiteSpace(q.Replace("@", string.Empty)))
			{
				return;
			}
			if (q.Length > 2000)
			{
				string text = q;
				int num2 = 2000 - 0;
				q = text.Substring(0, num2) + "...";
			}
			SendingAdminChatEventArgs sendingAdminChatEventArgs = new SendingAdminChatEventArgs(sender, q);
			ServerEvents.OnSendingAdminChat(sendingAdminChatEventArgs);
			if (!sendingAdminChatEventArgs.IsAllowed)
			{
				PlayerCommandSender playerCommandSender3 = sender as PlayerCommandSender;
				if (playerCommandSender3 != null)
				{
					playerCommandSender3.ReferenceHub.gameConsoleTransmission.SendToClient("A server plugin cancelled the message.", "red");
					playerCommandSender3.RaReply("A server plugin cancelled the message.", false, true, "");
				}
				return;
			}
			q = sendingAdminChatEventArgs.Message;
			string text2 = num.ToString() + "!" + q;
			if (ServerStatic.IsDedicated)
			{
				ServerConsole.AddLog("[AC " + sender.LogName + "] " + q, ConsoleColor.DarkYellow, false);
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, "[" + sender.LogName + "] " + q, ServerLogs.ServerLogType.AdminChat, false);
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				ClientInstanceMode mode = referenceHub2.Mode;
				if (mode != ClientInstanceMode.Unverified && mode != ClientInstanceMode.DedicatedServer && referenceHub2.serverRoles.AdminChatPerms)
				{
					referenceHub2.encryptedChannelManager.TrySendMessageToClient(text2, EncryptedChannelManager.EncryptedChannel.AdminChat);
				}
			}
			ServerEvents.OnSentAdminChat(new SentAdminChatEventArgs(sender, q));
		}

		internal static string ProcessQuery(string q, CommandSender sender)
		{
			if (q.StartsWith("$", StringComparison.Ordinal))
			{
				string[] array = q.Remove(0, 1).Split(' ', StringSplitOptions.None);
				if (array.Length <= 1)
				{
					return null;
				}
				int num;
				if (!int.TryParse(array[0], out num))
				{
					return null;
				}
				IServerCommunication serverCommunication;
				if (CommunicationProcessor.ServerCommunication.TryGetValue(num, out serverCommunication))
				{
					serverCommunication.ReceiveData(sender, string.Join(" ", array.Skip(1)));
				}
				return null;
			}
			else
			{
				string[] array2 = q.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
				ICommand command;
				bool flag = CommandProcessor.RemoteAdminCommandHandler.TryGetCommand(array2[0], out command);
				ArraySegment<string> arraySegment = array2.Segment(1);
				CommandExecutingEventArgs commandExecutingEventArgs = new CommandExecutingEventArgs(sender, CommandType.RemoteAdmin, flag, command, arraySegment);
				ServerEvents.OnCommandExecuting(commandExecutingEventArgs);
				if (!commandExecutingEventArgs.IsAllowed)
				{
					return null;
				}
				arraySegment = commandExecutingEventArgs.Arguments;
				sender = commandExecutingEventArgs.Sender;
				command = commandExecutingEventArgs.Command;
				if (flag)
				{
					try
					{
						string text;
						bool flag2 = command.Execute(array2.Segment(1), sender, out text);
						text = Misc.CloseAllRichTextTags(text);
						CommandExecutedEventArgs commandExecutedEventArgs = new CommandExecutedEventArgs(sender, CommandType.RemoteAdmin, command, arraySegment, flag2, text);
						ServerEvents.OnCommandExecuted(commandExecutedEventArgs);
						text = commandExecutedEventArgs.Response;
						flag2 = commandExecutedEventArgs.ExecutedSuccessfully;
						if (!string.IsNullOrEmpty(text))
						{
							sender.RaReply(array2[0].ToUpperInvariant() + "#" + text, flag2, true, "");
						}
						return text;
					}
					catch (Exception ex)
					{
						string text2 = "Command execution failed! Error: " + Misc.RemoveStacktraceZeroes(ex.ToString());
						CommandExecutedEventArgs commandExecutedEventArgs2 = new CommandExecutedEventArgs(sender, CommandType.RemoteAdmin, command, arraySegment, false, text2);
						ServerEvents.OnCommandExecuted(commandExecutedEventArgs2);
						text2 = commandExecutedEventArgs2.Response;
						sender.RaReply(text2, false, true, array2[0].ToUpperInvariant() + "#" + text2);
						return text2;
					}
				}
				sender.RaReply("SYSTEM#Unknown command!", false, true, string.Empty);
				return "Unknown command!";
			}
		}

		internal static float GetRoundedStat<T>(ReferenceHub hub) where T : StatBase
		{
			return Mathf.Round(hub.playerStats.GetModule<T>().CurValue * 100f) / 100f;
		}

		internal static List<ICommand> GetAllCommands()
		{
			return CommandProcessor.RemoteAdminCommandHandler.AllCommands.ToList<ICommand>();
		}

		private static bool CheckPermissions(CommandSender sender, string queryZero, PlayerPermissions perm, string replyScreen = "", bool reply = true)
		{
			if (ServerStatic.IsDedicated && sender.FullPermissions)
			{
				return true;
			}
			if (PermissionsHandler.IsPermitted(sender.Permissions, perm))
			{
				return true;
			}
			if (reply)
			{
				sender.RaReply(queryZero + "#You don't have permissions to execute this command.\nRequired permission: " + perm.ToString(), false, true, replyScreen);
			}
			return false;
		}

		internal static bool CheckPermissions(CommandSender sender, PlayerPermissions perm)
		{
			return (ServerStatic.IsDedicated && sender.FullPermissions) || PermissionsHandler.IsPermitted(sender.Permissions, perm);
		}

		private const int MaxStaffChatMessageLength = 2000;

		public static readonly RemoteAdminCommandHandler RemoteAdminCommandHandler = RemoteAdminCommandHandler.Create();
	}
}
