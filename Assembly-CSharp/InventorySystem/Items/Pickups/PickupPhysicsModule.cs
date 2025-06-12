using System;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Pickups;

public abstract class PickupPhysicsModule
{
	private static readonly byte[] ReaderBuffer = new byte[1024];

	private bool _wasSpawned;

	private SyncList<byte> SyncData => this.Pickup.PhysicsModuleSyncData;

	protected bool IsSpawned
	{
		get
		{
			if (this._wasSpawned)
			{
				return true;
			}
			if (!NetworkServer.spawned.ContainsKey(this.Pickup.netId))
			{
				return false;
			}
			this._wasSpawned = true;
			return true;
		}
	}

	protected abstract ItemPickupBase Pickup { get; }

	public virtual void DestroyModule()
	{
	}

	[Client]
	internal virtual void ClientProcessRpc(NetworkReader rpcData)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void InventorySystem.Items.Pickups.PickupPhysicsModule::ClientProcessRpc(Mirror.NetworkReader)' called when client was not active");
		}
	}

	[Server]
	protected void ServerSetSyncData(Action<NetworkWriter> writerMethod)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void InventorySystem.Items.Pickups.PickupPhysicsModule::ServerSetSyncData(System.Action`1<Mirror.NetworkWriter>)' called when server was not active");
			return;
		}
		using NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		writerMethod(networkWriterPooled);
		this.SyncData.Clear();
		for (int i = 0; i < networkWriterPooled.Position; i++)
		{
			this.SyncData.Add(networkWriterPooled.buffer[i]);
		}
	}

	[Client]
	protected void ClientReadSyncData(Action<NetworkReader> readerMethod)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void InventorySystem.Items.Pickups.PickupPhysicsModule::ClientReadSyncData(System.Action`1<Mirror.NetworkReader>)' called when client was not active");
			return;
		}
		int count = this.SyncData.Count;
		for (int i = 0; i < count; i++)
		{
			PickupPhysicsModule.ReaderBuffer[i] = this.SyncData[i];
		}
		using NetworkReaderPooled obj = NetworkReaderPool.Get(new ArraySegment<byte>(PickupPhysicsModule.ReaderBuffer, 0, count));
		readerMethod(obj);
	}

	[Server]
	protected void ServerSendRpc(Action<NetworkWriter> writerMethod)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void InventorySystem.Items.Pickups.PickupPhysicsModule::ServerSendRpc(System.Action`1<Mirror.NetworkWriter>)' called when server was not active");
		}
		else if (this.IsSpawned)
		{
			using (NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get())
			{
				writerMethod(networkWriterPooled);
				this.Pickup.SendPhysicsModuleRpc(networkWriterPooled.ToArraySegment());
			}
		}
	}
}
