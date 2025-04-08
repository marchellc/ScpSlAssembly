using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror
{
	public static class LiteNetLib4MirrorClient
	{
		public static string LastDisconnectReason { get; private set; }

		public static int GetPing()
		{
			return LiteNetLib4MirrorCore.Host.FirstPeer.Ping;
		}

		internal static bool IsConnected()
		{
			return LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.ClientConnected || LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.ClientConnecting;
		}

		internal static void ConnectClient(NetDataWriter data)
		{
			try
			{
				if (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Discovery)
				{
					LiteNetLib4MirrorCore.StopTransport();
				}
				EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
				LiteNetLib4MirrorCore.Host = new NetManager(eventBasedNetListener, null);
				eventBasedNetListener.NetworkReceiveEvent += LiteNetLib4MirrorClient.OnNetworkReceive;
				eventBasedNetListener.NetworkErrorEvent += LiteNetLib4MirrorClient.OnNetworkError;
				eventBasedNetListener.PeerConnectedEvent += LiteNetLib4MirrorClient.OnPeerConnected;
				eventBasedNetListener.PeerDisconnectedEvent += LiteNetLib4MirrorClient.OnPeerDisconnected;
				LiteNetLib4MirrorCore.SetOptions(false);
				LiteNetLib4MirrorCore.Host.Start();
				LiteNetLib4MirrorCore.Host.Connect(LiteNetLib4MirrorUtils.Parse(LiteNetLib4MirrorTransport.Singleton.clientAddress, LiteNetLib4MirrorTransport.Singleton.port), data);
				LiteNetLib4MirrorTransport.Polling = true;
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.ClientConnecting;
			}
			catch (Exception ex)
			{
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Idle;
				Debug.LogException(ex);
			}
		}

		private static void OnPeerConnected(NetPeer peer)
		{
			LiteNetLib4MirrorClient.LastDisconnectReason = null;
			LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.ClientConnected;
			LiteNetLib4MirrorTransport.Singleton.OnClientConnected();
		}

		private static void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
		{
			DisconnectReason reason = disconnectinfo.Reason;
			string text;
			if (reason != DisconnectReason.DisconnectPeerCalled)
			{
				if (reason == DisconnectReason.ConnectionRejected)
				{
					LiteNetLib4MirrorTransport.Singleton.OnConncetionRefused(disconnectinfo);
					LiteNetLib4MirrorClient.LastDisconnectReason = null;
					goto IL_004C;
				}
			}
			else if (disconnectinfo.AdditionalData.TryGetString(out text) && !string.IsNullOrWhiteSpace(text))
			{
				LiteNetLib4MirrorClient.LastDisconnectReason = LiteNetLib4MirrorUtils.FromBase64(text);
				goto IL_004C;
			}
			LiteNetLib4MirrorClient.LastDisconnectReason = null;
			IL_004C:
			LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Idle;
			LiteNetLib4MirrorCore.LastDisconnectError = disconnectinfo.SocketErrorCode;
			LiteNetLib4MirrorCore.LastDisconnectReason = disconnectinfo.Reason;
			LiteNetLib4MirrorTransport.Singleton.OnClientDisconnected();
			LiteNetLib4MirrorCore.StopTransport();
		}

		private static void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
		{
			LiteNetLib4MirrorTransport.Singleton.OnClientDataReceived(reader.GetRemainingBytesSegment(), -1);
			reader.Recycle();
		}

		private static void OnNetworkError(IPEndPoint endpoint, SocketError socketerror)
		{
			LiteNetLib4MirrorCore.LastError = socketerror;
			LiteNetLib4MirrorTransport.Singleton.OnClientError(TransportError.Unexpected, string.Format("Socket exception: {0}", (int)socketerror));
			LiteNetLib4MirrorTransport.Singleton.onClientSocketError.Invoke(socketerror);
		}

		internal static bool Send(DeliveryMethod method, byte[] data, int start, int length, byte channelNumber)
		{
			bool flag;
			try
			{
				LiteNetLib4MirrorCore.Host.FirstPeer.Send(data, start, length, channelNumber, method);
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
	}
}
