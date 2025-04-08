using System;
using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication
{
	public class RaClipboard : IClientCommunication
	{
		public int DataId
		{
			get
			{
				return 6;
			}
		}

		public void ReceiveData(string data, bool secure = true)
		{
		}

		public static void Send(CommandSender sender, RaClipboard.RaClipBoardType type, string data)
		{
			sender.RaReply(string.Format("$6 {0} {1}", (int)type, data), true, false, string.Empty);
		}

		public enum RaClipBoardType
		{
			Ip,
			UserId,
			PlayerId
		}
	}
}
