using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using LiteNetLib4Mirror.Open.Nat;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror
{
	public static class LiteNetLib4MirrorServer
	{
		public static string Code { get; internal set; }

		public static int GetPing(int id)
		{
			return LiteNetLib4MirrorServer.Peers[id].Ping;
		}

		internal static bool IsActive()
		{
			return LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Server;
		}

		internal static void StartServer(string code)
		{
			try
			{
				LiteNetLib4MirrorServer.Code = code;
				EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
				LiteNetLib4MirrorCore.Host = new NetManager(eventBasedNetListener, null);
				eventBasedNetListener.ConnectionRequestEvent += LiteNetLib4MirrorServer.OnConnectionRequest;
				eventBasedNetListener.PeerDisconnectedEvent += LiteNetLib4MirrorServer.OnPeerDisconnected;
				eventBasedNetListener.NetworkErrorEvent += LiteNetLib4MirrorServer.OnNetworkError;
				eventBasedNetListener.NetworkReceiveEvent += LiteNetLib4MirrorServer.OnNetworkReceive;
				eventBasedNetListener.PeerConnectedEvent += LiteNetLib4MirrorServer.OnPeerConnected;
				if (LiteNetLib4MirrorDiscovery.Singleton != null)
				{
					eventBasedNetListener.NetworkReceiveUnconnectedEvent += LiteNetLib4MirrorDiscovery.OnDiscoveryRequest;
				}
				LiteNetLib4MirrorCore.SetOptions(true);
				if (LiteNetLib4MirrorTransport.Singleton.useUpnP)
				{
					LiteNetLib4MirrorUtils.ForwardPort(NetworkProtocolType.Udp, 10000);
				}
				LiteNetLib4MirrorCore.Host.Start(LiteNetLib4MirrorUtils.Parse(LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress), LiteNetLib4MirrorUtils.Parse(LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress), (int)LiteNetLib4MirrorTransport.Singleton.port);
				LiteNetLib4MirrorServer.Peers = new NetPeer[(int)(LiteNetLib4MirrorTransport.Singleton.maxConnections * 2)];
				LiteNetLib4MirrorTransport.Polling = true;
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Server;
			}
			catch (Exception ex)
			{
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Idle;
				Debug.LogException(ex);
			}
		}

		private static void OnPeerConnected(NetPeer peer)
		{
			if (peer.Id + 1 > LiteNetLib4MirrorServer.Peers.Length)
			{
				Array.Resize<NetPeer>(ref LiteNetLib4MirrorServer.Peers, LiteNetLib4MirrorServer.Peers.Length * 2);
			}
			LiteNetLib4MirrorServer.Peers[peer.Id + 1] = peer;
			if (peer.Id + 1 > LiteNetLib4MirrorServer._maxId)
			{
				LiteNetLib4MirrorServer._maxId = peer.Id + 1;
			}
			LiteNetLib4MirrorTransport.Singleton.OnServerConnected(peer.Id + 1);
		}

		private static void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
		{
			LiteNetLib4MirrorTransport.Singleton.OnServerDataReceived(peer.Id + 1, reader.GetRemainingBytesSegment(), -1);
			reader.Recycle();
		}

		private static void OnNetworkError(IPEndPoint endpoint, SocketError socketerror)
		{
			LiteNetLib4MirrorCore.LastError = socketerror;
			for (int i = 0; i < LiteNetLib4MirrorServer._maxId; i++)
			{
				NetPeer netPeer = LiteNetLib4MirrorServer.Peers[i];
				if (netPeer != null && netPeer.EndPoint.Equals(endpoint))
				{
					LiteNetLib4MirrorTransport.Singleton.OnServerError(netPeer.Id + 1, TransportError.Unexpected, string.Format("Socket exception: {0}", (int)socketerror));
					LiteNetLib4MirrorTransport.Singleton.onServerSocketError.Invoke(netPeer.Id + 1, socketerror);
					return;
				}
			}
		}

		private static void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
		{
			LiteNetLib4MirrorCore.LastDisconnectError = disconnectinfo.SocketErrorCode;
			LiteNetLib4MirrorCore.LastDisconnectReason = disconnectinfo.Reason;
			LiteNetLib4MirrorTransport.Singleton.OnServerDisconnected(peer.Id + 1);
		}

		private static void OnConnectionRequest(ConnectionRequest request)
		{
			try
			{
				LiteNetLib4MirrorTransport.Singleton.ProcessConnectionRequest(request);
			}
			catch (Exception ex)
			{
				Debug.LogError("Malformed join request! Rejecting... Error:" + ex.Message + "\n" + ex.StackTrace);
				request.Reject();
			}
		}

		internal static bool Send(int connectionId, DeliveryMethod method, byte[] data, int start, int length, byte channelNumber)
		{
			bool flag;
			try
			{
				LiteNetLib4MirrorServer.Peers[connectionId].Send(data, start, length, channelNumber, method);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		internal static bool Disconnect(int connectionId)
		{
			bool flag;
			try
			{
				if (LiteNetLib4MirrorServer.DisconnectMessage == null)
				{
					LiteNetLib4MirrorServer.Peers[connectionId].Disconnect();
				}
				else
				{
					LiteNetLib4MirrorServer.Peers[connectionId].Disconnect(LiteNetLib4MirrorUtils.ReusePut(LiteNetLib4MirrorServer.Writer, LiteNetLib4MirrorServer.DisconnectMessage, ref LiteNetLib4MirrorServer._lastMessage));
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		internal static string GetClientAddress(int connectionId)
		{
			return LiteNetLib4MirrorServer.Peers[connectionId].EndPoint.Address.ToString();
		}

		public static NetPeer[] Peers;

		private static int _maxId;

		internal static string DisconnectMessage = null;

		private static readonly NetDataWriter Writer = new NetDataWriter();

		private static string _lastMessage;
	}
}
