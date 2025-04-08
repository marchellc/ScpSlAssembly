using System;
using System.Collections.Generic;
using RemoteAdmin.Interfaces;
using Utils;

namespace RemoteAdmin.Communication
{
	public class RaPlayerAuth : IServerCommunication
	{
		public int DataId
		{
			get
			{
				return 3;
			}
		}

		public void ReceiveData(CommandSender sender, string data)
		{
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender != null && !playerCommandSender.ReferenceHub.authManager.RemoteAdminGlobalAccess && !CommandProcessor.CheckPermissions(sender, PlayerPermissions.PlayerSensitiveDataAccess))
			{
				return;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(new ArraySegment<string>(data.Split(' ', StringSplitOptions.None)), 0, out array, false);
			if (list.Count == 0)
			{
				return;
			}
			if (list.Count > 1)
			{
				return;
			}
			if (list[0].authManager.AuthenticationResponse.AuthToken == null)
			{
				sender.RaReply("PlayerInfo#Can't obtain auth token. Is server using offline mode or you selected the host?", false, true, "PlayerInfo");
				return;
			}
			ServerLogs.AddLog(ServerLogs.Modules.DataAccess, string.Format("{0} accessed authentication token of player {1} ({2}).", sender.LogName, list[0].PlayerId, list[0].nicknameSync.MyNick), ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
			sender.RaReply("PlayerInfo#<color=white>Displayed Authentication token of player " + list[0].LoggedNameFromRefHub() + "</color>", true, true, "null");
			RaPlayerQR.Send(sender, true, list[0].authManager.GetAuthToken());
		}
	}
}
