using System;
using System.Linq;
using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication
{
	public class RaGlobalBan : IServerCommunication, IClientCommunication
	{
		public int DataId
		{
			get
			{
				return 5;
			}
		}

		public void ReceiveData(CommandSender sender, string data)
		{
			string[] array = data.Split(' ', StringSplitOptions.None);
			if (array.Length < 2)
			{
				return;
			}
			int num;
			if (!int.TryParse(array[0], out num))
			{
				return;
			}
			bool flag = num == 1;
			data = string.Join(" ", array.Skip(1));
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null || !playerCommandSender.ReferenceHub.authManager.RemoteAdminGlobalAccess)
			{
				return;
			}
			ReferenceHub referenceHub = null;
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				if ((flag && referenceHub2.PlayerId.ToString() == data) || (!flag && string.Equals(referenceHub2.nicknameSync.MyNick, data, StringComparison.OrdinalIgnoreCase)))
				{
					referenceHub = referenceHub2;
					break;
				}
			}
			if (referenceHub == null || referenceHub.authManager.AuthenticationResponse.SignedAuthToken == null)
			{
				sender.RaReply(string.Format("${0} 0", this.DataId), true, false, string.Empty);
				return;
			}
			sender.RaReply(string.Format("${0} 1 {1}", this.DataId, referenceHub.authManager.GetAuthToken()), true, false, string.Empty);
		}

		public void ReceiveData(string data, bool secure)
		{
		}
	}
}
