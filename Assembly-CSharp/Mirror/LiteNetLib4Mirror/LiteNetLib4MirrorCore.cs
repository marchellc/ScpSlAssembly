using System.Net.Sockets;
using LiteNetLib;

namespace Mirror.LiteNetLib4Mirror;

public static class LiteNetLib4MirrorCore
{
	public enum States : byte
	{
		NonInitialized,
		Idle,
		Discovery,
		ClientConnecting,
		ClientConnected,
		Server
	}

	public const string TransportVersion = "1.2.8";

	public static SocketError LastError { get; internal set; }

	public static SocketError LastDisconnectError { get; internal set; }

	public static DisconnectReason LastDisconnectReason { get; internal set; }

	public static NetManager Host { get; internal set; }

	public static States State { get; internal set; }

	internal static string GetState()
	{
		return LiteNetLib4MirrorCore.State switch
		{
			States.NonInitialized => "LiteNetLib4Mirror isn't initialized", 
			States.Idle => "LiteNetLib4Mirror Transport idle", 
			States.ClientConnecting => $"LiteNetLib4Mirror Client Connecting to {LiteNetLib4MirrorTransport.Singleton.clientAddress}:{LiteNetLib4MirrorTransport.Singleton.port}", 
			States.ClientConnected => $"LiteNetLib4Mirror Client Connected to {LiteNetLib4MirrorTransport.Singleton.clientAddress}:{LiteNetLib4MirrorTransport.Singleton.port}", 
			States.Server => $"LiteNetLib4Mirror Server active at IPv4:{LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress} IPv6:{LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress} Port:{LiteNetLib4MirrorTransport.Singleton.port}", 
			_ => "Invalid state!", 
		};
	}

	internal static void SetOptions(bool server)
	{
		LiteNetLib4MirrorCore.Host.IPv6Enabled = LiteNetLib4MirrorTransport.Singleton.ipv6Enabled;
		LiteNetLib4MirrorCore.Host.UpdateTime = LiteNetLib4MirrorTransport.Singleton.updateTime;
		LiteNetLib4MirrorCore.Host.PingInterval = LiteNetLib4MirrorTransport.Singleton.pingInterval;
		LiteNetLib4MirrorCore.Host.DisconnectTimeout = LiteNetLib4MirrorTransport.Singleton.disconnectTimeout;
		LiteNetLib4MirrorCore.Host.ReconnectDelay = LiteNetLib4MirrorTransport.Singleton.reconnectDelay;
		LiteNetLib4MirrorCore.Host.MaxConnectAttempts = LiteNetLib4MirrorTransport.Singleton.maxConnectAttempts;
		LiteNetLib4MirrorCore.Host.UseNativeSockets = LiteNetLib4MirrorTransport.Singleton.useNativeSockets;
		LiteNetLib4MirrorCore.Host.SimulatePacketLoss = LiteNetLib4MirrorTransport.Singleton.simulatePacketLoss;
		LiteNetLib4MirrorCore.Host.SimulationPacketLossChance = LiteNetLib4MirrorTransport.Singleton.simulationPacketLossChance;
		LiteNetLib4MirrorCore.Host.SimulateLatency = LiteNetLib4MirrorTransport.Singleton.simulateLatency;
		LiteNetLib4MirrorCore.Host.SimulationMinLatency = LiteNetLib4MirrorTransport.Singleton.simulationMinLatency;
		LiteNetLib4MirrorCore.Host.SimulationMaxLatency = LiteNetLib4MirrorTransport.Singleton.simulationMaxLatency;
		LiteNetLib4MirrorCore.Host.BroadcastReceiveEnabled = server && LiteNetLib4MirrorDiscovery.Singleton != null;
		LiteNetLib4MirrorCore.Host.ChannelsCount = (byte)LiteNetLib4MirrorTransport.Singleton.channels.Length;
	}

	internal static void StopTransport()
	{
		if (LiteNetLib4MirrorCore.Host != null)
		{
			LiteNetLib4MirrorServer.Peers = null;
			LiteNetLib4MirrorCore.Host.Stop();
			LiteNetLib4MirrorCore.Host = null;
			LiteNetLib4MirrorTransport.Polling = false;
			LiteNetLib4MirrorCore.State = States.Idle;
		}
	}

	internal static int GetMaxPacketSize(DeliveryMethod channel)
	{
		int num = ((LiteNetLib4MirrorCore.Host != null && LiteNetLib4MirrorCore.Host.FirstPeer != null) ? LiteNetLib4MirrorCore.Host.FirstPeer.Mtu : NetConstants.MaxPacketSize);
		switch (channel)
		{
		case DeliveryMethod.ReliableUnordered:
		case DeliveryMethod.ReliableOrdered:
			return 65535 * (num - 6);
		case DeliveryMethod.Sequenced:
		case DeliveryMethod.ReliableSequenced:
			return num - 4;
		case DeliveryMethod.Unreliable:
			return num - 1;
		default:
			return num - 1;
		}
	}
}
