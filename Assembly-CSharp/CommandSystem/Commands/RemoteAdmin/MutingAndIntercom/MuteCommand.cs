using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using RemoteAdmin;
using Utils;
using VoiceChat;

namespace CommandSystem.Commands.RemoteAdmin.MutingAndIntercom
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class MuteCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "mute";

		public string[] Aliases { get; }

		public string Description { get; } = "Prevents the specified player(s) from being able to speak.";

		public string[] Usage { get; } = new string[] { "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(new PlayerPermissions[]
			{
				PlayerPermissions.BanningUpToDay,
				PlayerPermissions.LongTermBanning,
				PlayerPermissions.PlayersManagement
			}, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			ReferenceHub referenceHub = ((playerCommandSender != null) ? playerCommandSender.ReferenceHub : ReferenceHub.HostHub);
			int num = 0;
			if (list != null)
			{
				foreach (ReferenceHub referenceHub2 in list)
				{
					PlayerMutingEventArgs playerMutingEventArgs = new PlayerMutingEventArgs(referenceHub2, referenceHub, false);
					PlayerEvents.OnMuting(playerMutingEventArgs);
					if (playerMutingEventArgs.IsAllowed)
					{
						VoiceChatMutes.IssueLocalMute(referenceHub2.authManager.UserId, false);
						PlayerEvents.OnMuted(new PlayerMutedEventArgs(referenceHub2, referenceHub, false));
						ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " muted player " + referenceHub2.LoggedNameFromRefHub() + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
						num++;
					}
				}
			}
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}
	}
}
