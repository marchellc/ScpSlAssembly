using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror;

[RequireComponent(typeof(LiteNetLib4MirrorTransport))]
public class LiteNetLib4MirrorDiscovery : MonoBehaviour
{
	public UnityEventIpEndpointString onDiscoveryResponse;

	public ushort[] ports = new ushort[1] { 7777 };

	private static readonly NetDataWriter DataWriter = new NetDataWriter();

	private static string _lastDiscoveryMessage;

	public static LiteNetLib4MirrorDiscovery Singleton { get; protected set; }

	protected virtual void Awake()
	{
		if (Singleton == null)
		{
			GetComponent<LiteNetLib4MirrorTransport>().InitializeTransport();
			Singleton = this;
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
			LiteNetLib4MirrorCore.Host = new NetManager(eventBasedNetListener);
			eventBasedNetListener.NetworkReceiveUnconnectedEvent += OnDiscoveryResponse;
			LiteNetLib4MirrorCore.Host.UnconnectedMessagesEnabled = true;
			LiteNetLib4MirrorCore.Host.Start();
			LiteNetLib4MirrorCore.State = LiteNetLib4MirrorCore.States.Discovery;
			LiteNetLib4MirrorTransport.Polling = true;
		}
		else
		{
			Debug.LogWarning("LiteNetLib4Mirror is already running as a client or a server!");
		}
	}

	public static void SendDiscoveryRequest(string text)
	{
		if (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.Discovery)
		{
			LiteNetLib4MirrorUtils.ReusePutDiscovery(DataWriter, text, ref _lastDiscoveryMessage);
			ushort[] array = Singleton.ports;
			foreach (ushort port in array)
			{
				LiteNetLib4MirrorCore.Host.SendBroadcast(DataWriter, port);
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
		if (messagetype == UnconnectedMessageType.BasicMessage && reader.TryGetString(out var result) && result == Application.productName)
		{
			Singleton.onDiscoveryResponse.Invoke(remoteendpoint, LiteNetLib4MirrorUtils.FromBase64(reader.GetString()));
		}
		reader.Recycle();
	}

	internal static void OnDiscoveryRequest(IPEndPoint remoteendpoint, NetPacketReader reader, UnconnectedMessageType messagetype)
	{
		if (messagetype == UnconnectedMessageType.Broadcast && reader.TryGetString(out var result) && result == Application.productName && Singleton.ProcessDiscoveryRequest(remoteendpoint, LiteNetLib4MirrorUtils.FromBase64(reader.GetString()), out var response))
		{
			LiteNetLib4MirrorCore.Host.SendUnconnectedMessage(LiteNetLib4MirrorUtils.ReusePutDiscovery(DataWriter, response, ref _lastDiscoveryMessage), remoteendpoint);
		}
		reader.Recycle();
	}
}
