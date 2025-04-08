using System;
using System.Runtime.InteropServices;
using Interactables.Interobjects;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class SqueakSpawner : NetworkBehaviour
{
	private void Awake()
	{
		if (NetworkServer.active && global::UnityEngine.Random.Range(0, 100) <= this.spawnChancePercent)
		{
			this.NetworksyncSpawn = (byte)global::UnityEngine.Random.Range(1, this.mice.Length + 1);
			this.SyncMouseSpawn(0, this.syncSpawn);
		}
	}

	[TargetRpc]
	public void TargetHitMouse(NetworkConnection networkConnection)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendTargetRPCInternal(networkConnection, "System.Void SqueakSpawner::TargetHitMouse(Mirror.NetworkConnection)", 1242807795, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private void SyncMouseSpawn(byte oldValue, byte newValue)
	{
		if (newValue == 0)
		{
			return;
		}
		GameObject gameObject = this.mice[(int)(newValue - 1)];
		gameObject.SetActive(true);
		this._spawnedMouse = gameObject.GetComponent<SqueakInteraction>();
	}

	public override bool Weaved()
	{
		return true;
	}

	public byte NetworksyncSpawn
	{
		get
		{
			return this.syncSpawn;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<byte>(value, ref this.syncSpawn, 1UL, new Action<byte, byte>(this.SyncMouseSpawn));
		}
	}

	protected void UserCode_TargetHitMouse__NetworkConnection(NetworkConnection networkConnection)
	{
	}

	protected static void InvokeUserCode_TargetHitMouse__NetworkConnection(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetHitMouse called on server.");
			return;
		}
		((SqueakSpawner)obj).UserCode_TargetHitMouse__NetworkConnection(null);
	}

	static SqueakSpawner()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(SqueakSpawner), "System.Void SqueakSpawner::TargetHitMouse(Mirror.NetworkConnection)", new RemoteCallDelegate(SqueakSpawner.InvokeUserCode_TargetHitMouse__NetworkConnection));
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteByte(this.syncSpawn);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteByte(this.syncSpawn);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this.syncSpawn, new Action<byte, byte>(this.SyncMouseSpawn), reader.ReadByte());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this.syncSpawn, new Action<byte, byte>(this.SyncMouseSpawn), reader.ReadByte());
		}
	}

	[SerializeField]
	private int spawnChancePercent = 10;

	[SerializeField]
	private GameObject[] mice;

	[SyncVar(hook = "SyncMouseSpawn")]
	private byte syncSpawn;

	private SqueakInteraction _spawnedMouse;
}
