using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication;

public class RaPlayerQR : IClientCommunication
{
	public int DataId => 2;

	public void ReceiveData(string data, bool secure)
	{
	}

	public static void Send(CommandSender sender, bool isBig, string data)
	{
		sender.RaReply($"$2 {(isBig ? 1 : 0)} {data}", success: true, logToConsole: false, string.Empty);
	}
}
