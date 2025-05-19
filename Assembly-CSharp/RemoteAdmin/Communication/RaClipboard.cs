using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication;

public class RaClipboard : IClientCommunication
{
	public enum RaClipBoardType
	{
		Ip,
		UserId,
		PlayerId
	}

	public int DataId => 6;

	public void ReceiveData(string data, bool secure = true)
	{
	}

	public static void Send(CommandSender sender, RaClipBoardType type, string data)
	{
		sender.RaReply($"$6 {(int)type} {data}", success: true, logToConsole: false, string.Empty);
	}
}
