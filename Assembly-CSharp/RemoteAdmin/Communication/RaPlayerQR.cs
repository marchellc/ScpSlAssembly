using System;
using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication
{
	public class RaPlayerQR : IClientCommunication
	{
		public int DataId
		{
			get
			{
				return 2;
			}
		}

		public void ReceiveData(string data, bool secure)
		{
		}

		public static void Send(CommandSender sender, bool isBig, string data)
		{
			sender.RaReply(string.Format("$2 {0} {1}", isBig ? 1 : 0, data), true, false, string.Empty);
		}
	}
}
