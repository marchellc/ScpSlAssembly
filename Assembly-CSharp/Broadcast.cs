using System;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class Broadcast : NetworkBehaviour
{
	public static Broadcast Singleton
	{
		get
		{
			if (!Broadcast._broadcastSet)
			{
				Broadcast._broadcastSet = true;
				Broadcast._broadcast = ReferenceHub.LocalHub.GetComponent<Broadcast>();
			}
			return Broadcast._broadcast;
		}
	}

	private void Start()
	{
	}

	private void OnDestroy()
	{
		if (this == Broadcast._broadcast)
		{
			Broadcast._broadcastSet = false;
		}
	}

	[TargetRpc]
	public void TargetAddElement(NetworkConnection conn, string data, ushort time, Broadcast.BroadcastFlags flags)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(data);
		networkWriterPooled.WriteUShort(time);
		global::Mirror.GeneratedNetworkCode._Write_Broadcast/BroadcastFlags(networkWriterPooled, flags);
		this.SendTargetRPCInternal(conn, "System.Void Broadcast::TargetAddElement(Mirror.NetworkConnection,System.String,System.UInt16,Broadcast/BroadcastFlags)", 624805019, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	public void RpcAddElement(string data, ushort time, Broadcast.BroadcastFlags flags)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(data);
		networkWriterPooled.WriteUShort(time);
		global::Mirror.GeneratedNetworkCode._Write_Broadcast/BroadcastFlags(networkWriterPooled, flags);
		this.SendRPCInternal("System.Void Broadcast::RpcAddElement(System.String,System.UInt16,Broadcast/BroadcastFlags)", -775219482, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[TargetRpc]
	public void TargetClearElements(NetworkConnection conn)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendTargetRPCInternal(conn, "System.Void Broadcast::TargetClearElements(Mirror.NetworkConnection)", -1104952326, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	public void RpcClearElements()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Broadcast::RpcClearElements()", -701809763, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(NetworkConnection conn, string data, ushort time, Broadcast.BroadcastFlags flags)
	{
	}

	protected static void InvokeUserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetAddElement called on server.");
			return;
		}
		((Broadcast)obj).UserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(null, reader.ReadString(), reader.ReadUShort(), global::Mirror.GeneratedNetworkCode._Read_Broadcast/BroadcastFlags(reader));
	}

	protected void UserCode_RpcAddElement__String__UInt16__BroadcastFlags(string data, ushort time, Broadcast.BroadcastFlags flags)
	{
	}

	protected static void InvokeUserCode_RpcAddElement__String__UInt16__BroadcastFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAddElement called on server.");
			return;
		}
		((Broadcast)obj).UserCode_RpcAddElement__String__UInt16__BroadcastFlags(reader.ReadString(), reader.ReadUShort(), global::Mirror.GeneratedNetworkCode._Read_Broadcast/BroadcastFlags(reader));
	}

	protected void UserCode_TargetClearElements__NetworkConnection(NetworkConnection conn)
	{
	}

	protected static void InvokeUserCode_TargetClearElements__NetworkConnection(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetClearElements called on server.");
			return;
		}
		((Broadcast)obj).UserCode_TargetClearElements__NetworkConnection(null);
	}

	protected void UserCode_RpcClearElements()
	{
	}

	protected static void InvokeUserCode_RpcClearElements(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClearElements called on server.");
			return;
		}
		((Broadcast)obj).UserCode_RpcClearElements();
	}

	static Broadcast()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::RpcAddElement(System.String,System.UInt16,Broadcast/BroadcastFlags)", new RemoteCallDelegate(Broadcast.InvokeUserCode_RpcAddElement__String__UInt16__BroadcastFlags));
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::RpcClearElements()", new RemoteCallDelegate(Broadcast.InvokeUserCode_RpcClearElements));
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::TargetAddElement(Mirror.NetworkConnection,System.String,System.UInt16,Broadcast/BroadcastFlags)", new RemoteCallDelegate(Broadcast.InvokeUserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags));
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::TargetClearElements(Mirror.NetworkConnection)", new RemoteCallDelegate(Broadcast.InvokeUserCode_TargetClearElements__NetworkConnection));
	}

	private static Broadcast _broadcast;

	private static bool _broadcastSet;

	[Flags]
	public enum BroadcastFlags : byte
	{
		Normal = 0,
		Truncated = 1,
		AdminChat = 2
	}
}
