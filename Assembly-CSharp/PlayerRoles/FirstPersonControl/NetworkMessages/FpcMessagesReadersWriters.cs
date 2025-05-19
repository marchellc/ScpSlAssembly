using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages;

public static class FpcMessagesReadersWriters
{
	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler((Action<FpcPositionMessage>)delegate
			{
			}, requireAuthentication: true);
			NetworkClient.ReplaceHandler(delegate(FpcPositionOverrideMessage msg)
			{
				msg.ProcessMessage();
			});
			NetworkClient.ReplaceHandler(delegate(FpcRotationOverrideMessage msg)
			{
				msg.ProcessMessage();
			});
			NetworkClient.ReplaceHandler(delegate(FpcFallDamageMessage msg)
			{
				msg.ProcessMessage();
			});
			NetworkServer.ReplaceHandler(delegate(NetworkConnectionToClient conn, FpcFromClientMessage msg)
			{
				msg.ProcessMessage(conn);
			});
			NetworkServer.ReplaceHandler(delegate(NetworkConnectionToClient conn, FpcNoclipToggleMessage msg)
			{
				msg.ProcessMessage(conn);
			});
		};
	}

	public static void WriteFpcFromClientMessage(this NetworkWriter writer, FpcFromClientMessage msg)
	{
		msg.Write(writer);
	}

	public static FpcFromClientMessage ReadFpcFromClientMessage(this NetworkReader reader)
	{
		return new FpcFromClientMessage(reader);
	}

	public static void WriteFpcPositionMessage(this NetworkWriter writer, FpcPositionMessage msg)
	{
		msg.Write(writer);
	}

	public static FpcPositionMessage ReadFpcPositionMessage(this NetworkReader reader)
	{
		return new FpcPositionMessage(reader);
	}

	public static void WriteFpcPositionOverrideMessage(this NetworkWriter writer, FpcPositionOverrideMessage msg)
	{
		msg.Write(writer);
	}

	public static FpcPositionOverrideMessage ReadFpcPositionOverrideMessage(this NetworkReader reader)
	{
		return new FpcPositionOverrideMessage(reader);
	}

	public static void WriteFpcRotationOverrideMessage(this NetworkWriter writer, FpcPositionOverrideMessage msg)
	{
		msg.Write(writer);
	}

	public static FpcPositionOverrideMessage ReadFpcRotationOverrideMessage(this NetworkReader reader)
	{
		return new FpcPositionOverrideMessage(reader);
	}

	public static void WriteFpcFallDamageMessage(this NetworkWriter writer, FpcFallDamageMessage msg)
	{
		msg.Write(writer);
	}

	public static FpcFallDamageMessage ReadFpcFallDamageMessage(this NetworkReader reader)
	{
		return new FpcFallDamageMessage(reader);
	}
}
