using System;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class Broadcast : NetworkBehaviour
{
	[Flags]
	public enum BroadcastFlags : byte
	{
		Normal = 0,
		Truncated = 1,
		AdminChat = 2
	}

	private static Broadcast _broadcast;

	private static bool _broadcastSet;

	public static Broadcast Singleton
	{
		get
		{
			if (!_broadcastSet)
			{
				_broadcastSet = true;
				_broadcast = ReferenceHub.LocalHub.GetComponent<Broadcast>();
			}
			return _broadcast;
		}
	}

	private void Start()
	{
	}

	private void OnDestroy()
	{
		if (this == _broadcast)
		{
			_broadcastSet = false;
		}
	}

	[TargetRpc]
	public void TargetAddElement(NetworkConnection conn, string data, ushort time, BroadcastFlags flags)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(data);
		writer.WriteUShort(time);
		GeneratedNetworkCode._Write_Broadcast_002FBroadcastFlags(writer, flags);
		SendTargetRPCInternal(conn, "System.Void Broadcast::TargetAddElement(Mirror.NetworkConnection,System.String,System.UInt16,Broadcast/BroadcastFlags)", 624805019, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	public void RpcAddElement(string data, ushort time, BroadcastFlags flags)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(data);
		writer.WriteUShort(time);
		GeneratedNetworkCode._Write_Broadcast_002FBroadcastFlags(writer, flags);
		SendRPCInternal("System.Void Broadcast::RpcAddElement(System.String,System.UInt16,Broadcast/BroadcastFlags)", -775219482, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	public void TargetClearElements(NetworkConnection conn)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		SendTargetRPCInternal(conn, "System.Void Broadcast::TargetClearElements(Mirror.NetworkConnection)", -1104952326, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	public void RpcClearElements()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		SendRPCInternal("System.Void Broadcast::RpcClearElements()", -701809763, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(NetworkConnection conn, string data, ushort time, BroadcastFlags flags)
	{
	}

	protected static void InvokeUserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetAddElement called on server.");
		}
		else
		{
			((Broadcast)obj).UserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags(null, reader.ReadString(), reader.ReadUShort(), GeneratedNetworkCode._Read_Broadcast_002FBroadcastFlags(reader));
		}
	}

	protected void UserCode_RpcAddElement__String__UInt16__BroadcastFlags(string data, ushort time, BroadcastFlags flags)
	{
	}

	protected static void InvokeUserCode_RpcAddElement__String__UInt16__BroadcastFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAddElement called on server.");
		}
		else
		{
			((Broadcast)obj).UserCode_RpcAddElement__String__UInt16__BroadcastFlags(reader.ReadString(), reader.ReadUShort(), GeneratedNetworkCode._Read_Broadcast_002FBroadcastFlags(reader));
		}
	}

	protected void UserCode_TargetClearElements__NetworkConnection(NetworkConnection conn)
	{
	}

	protected static void InvokeUserCode_TargetClearElements__NetworkConnection(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetClearElements called on server.");
		}
		else
		{
			((Broadcast)obj).UserCode_TargetClearElements__NetworkConnection(null);
		}
	}

	protected void UserCode_RpcClearElements()
	{
	}

	protected static void InvokeUserCode_RpcClearElements(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClearElements called on server.");
		}
		else
		{
			((Broadcast)obj).UserCode_RpcClearElements();
		}
	}

	static Broadcast()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::RpcAddElement(System.String,System.UInt16,Broadcast/BroadcastFlags)", InvokeUserCode_RpcAddElement__String__UInt16__BroadcastFlags);
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::RpcClearElements()", InvokeUserCode_RpcClearElements);
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::TargetAddElement(Mirror.NetworkConnection,System.String,System.UInt16,Broadcast/BroadcastFlags)", InvokeUserCode_TargetAddElement__NetworkConnection__String__UInt16__BroadcastFlags);
		RemoteProcedureCalls.RegisterRpc(typeof(Broadcast), "System.Void Broadcast::TargetClearElements(Mirror.NetworkConnection)", InvokeUserCode_TargetClearElements__NetworkConnection);
	}
}
