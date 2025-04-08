using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror
{
	[RequireComponent(typeof(LiteNetLib4MirrorTransport))]
	public class LiteNetLib4MirrorDiscovery : MonoBehaviour
	{
		public static LiteNetLib4MirrorDiscovery Singleton { get; protected set; }

		protected virtual void Awake()
		{
			if (LiteNetLib4MirrorDiscovery.Singleton == null)
			{
				base.GetComponent<LiteNetLib4MirrorTransport>().InitializeTransport();
				LiteNetLib4MirrorDiscovery.Singleton = this;
			}
		}

		protected virtual bool ProcessDiscoveryRequest(IPEndPoint ipEndPoint, string text, out string response)
		{
			response = "LiteNetLib4Mirror Discovery accepted";
			return true;
		}

		public static void InitializeFinder()
		{
			if (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Idle)
			{
				EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
				LiteNetLib4MirrorCore.Host = new NetManager(eventBasedNetListener, null);
				eventBasedNetListener.NetworkReceiveUnconnectedEvent += LiteNetLib4MirrorDiscovery.OnDiscoveryResponse;
				LiteNetLib4MirrorCore.Host.UnconnectedMessagesEnabled = true;
				LiteNetLib4MirrorCore.Host.Start();
				LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Discovery;
				LiteNetLib4MirrorTransport.Polling = true;
				return;
			}
			Debug.LogWarning("LiteNetLib4Mirror is already running as a client or a server!");
		}

		public static void SendDiscoveryRequest(string text)
		{
			if (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Discovery)
			{
				LiteNetLib4MirrorUtils.ReusePutDiscovery(LiteNetLib4MirrorDiscovery.DataWriter, text, ref LiteNetLib4MirrorDiscovery._lastDiscoveryMessage);
				foreach (ushort num in LiteNetLib4MirrorDiscovery.Singleton.ports)
				{
					LiteNetLib4MirrorCore.Host.SendBroadcast(LiteNetLib4MirrorDiscovery.DataWriter, (int)num);
				}
			}
		}

		public static void StopDiscovery()
		{
			if (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Discovery)
			{
				LiteNetLib4MirrorCore.StopTransport();
			}
		}

		private static void OnDiscoveryResponse(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
		{
			string text;
			if (messagetype == UnconnectedMessageType.BasicMessage && reader.TryGetString(out text) && text == Application.productName)
			{
				LiteNetLib4MirrorDiscovery.Singleton.onDiscoveryResponse.Invoke(remoteendpoint, LiteNetLib4MirrorUtils.FromBase64(reader.GetString()));
			}
			reader.Recycle();
		}

		internal static void OnDiscoveryRequest(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
		{
			string text;
			string text2;
			if (messagetype == UnconnectedMessageType.Broadcast && reader.TryGetString(out text) && text == Application.productName && LiteNetLib4MirrorDiscovery.Singleton.ProcessDiscoveryRequest(remoteendpoint, LiteNetLib4MirrorUtils.FromBase64(reader.GetString()), out text2))
			{
				LiteNetLib4MirrorCore.Host.SendUnconnectedMessage(LiteNetLib4MirrorUtils.ReusePutDiscovery(LiteNetLib4MirrorDiscovery.DataWriter, text2, ref LiteNetLib4MirrorDiscovery._lastDiscoveryMessage), remoteendpoint);
			}
			reader.Recycle();
		}

		public UnityEventIpEndpointString onDiscoveryResponse;

		public ushort[] ports = new ushort[] { 7777 };

		private static readonly NetDataWriter DataWriter = new NetDataWriter();

		private static string _lastDiscoveryMessage;
	}
}
