using System;
using System.Net.Sockets;
using LiteNetLib;

namespace Mirror.LiteNetLib4Mirror
{
	public static class LiteNetLib4MirrorCore
	{
		public static SocketError LastError { get; internal set; }

		public static SocketError LastDisconnectError { get; internal set; }

		public static DisconnectReason LastDisconnectReason { get; internal set; }

		public static NetManager Host { get; internal set; }

		public static LiteNetLib4MirrorCore.States State { get; internal set; }

		internal static string GetState()
		{
			switch (LiteNetLib4MirrorCore.State)
			{
			case LiteNetLib4MirrorCore.States.NonInitialized:
				return "LiteNetLib4Mirror isn't initialized";
			case LiteNetLib4MirrorCore.States.Idle:
				return "LiteNetLib4Mirror Transport idle";
			case LiteNetLib4MirrorCore.States.ClientConnecting:
				return string.Format("LiteNetLib4Mirror Client Connecting to {0}:{1}", LiteNetLib4MirrorTransport.Singleton.clientAddress, LiteNetLib4MirrorTransport.Singleton.port);
			case LiteNetLib4MirrorCore.States.ClientConnected:
				return string.Format("LiteNetLib4Mirror Client Connected to {0}:{1}", LiteNetLib4MirrorTransport.Singleton.clientAddress, LiteNetLib4MirrorTransport.Singleton.port);
			case LiteNetLib4MirrorCore.States.Server:
				return string.Format("LiteNetLib4Mirror Server active at IPv4:{0} IPv6:{1} Port:{2}", LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress, LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress, LiteNetLib4MirrorTransport.Singleton.port);
			}
			return "Invalid state!";
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
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Idle;
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

		public const string TransportVersion = "1.2.8";

		public enum States : byte
		{
			NonInitialized,
			Idle,
			Discovery,
			ClientConnecting,
			ClientConnected,
			Server
		}
	}
}
