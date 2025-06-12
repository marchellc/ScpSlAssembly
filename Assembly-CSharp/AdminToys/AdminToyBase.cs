using System;
using System.Runtime.InteropServices;
using Footprinting;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace AdminToys;

public abstract class AdminToyBase : NetworkBehaviour
{
	[SyncVar]
	public Vector3 Position;

	[SyncVar]
	public Quaternion Rotation;

	[SyncVar]
	public Vector3 Scale;

	[SyncVar]
	public byte MovementSmoothing;

	[SyncVar]
	public bool IsStatic;

	private uint _clientParentId;

	private Transform _previousParent;

	private const float SmoothingMultiplier = 0.3f;

	public abstract string CommandName { get; }

	public Footprint SpawnerFootprint { get; private set; }

	public Vector3 RotationEuler
	{
		get
		{
			return this.Rotation.eulerAngles;
		}
		set
		{
			this.NetworkRotation = Quaternion.Euler(value);
		}
	}

	public Vector3 NetworkPosition
	{
		get
		{
			return this.Position;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Position, 1uL, null);
		}
	}

	public Quaternion NetworkRotation
	{
		get
		{
			return this.Rotation;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Rotation, 2uL, null);
		}
	}

	public Vector3 NetworkScale
	{
		get
		{
			return this.Scale;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Scale, 4uL, null);
		}
	}

	public byte NetworkMovementSmoothing
	{
		get
		{
			return this.MovementSmoothing;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.MovementSmoothing, 8uL, null);
		}
	}

	public bool NetworkIsStatic
	{
		get
		{
			return this.IsStatic;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.IsStatic, 16uL, null);
		}
	}

	public static event Action<AdminToyBase> OnAdded;

	public static event Action<AdminToyBase> OnRemoved;

	protected virtual void Start()
	{
		AdminToyBase.OnAdded?.Invoke(this);
	}

	protected virtual void LateUpdate()
	{
		if (!this.IsStatic)
		{
			if (NetworkServer.active)
			{
				this.UpdatePositionServer();
			}
			else
			{
				this.UpdatePositionClient();
			}
		}
	}

	protected void OnTransformParentChanged()
	{
		if (base.netIdentity.isServer)
		{
			Transform parent = base.transform.parent;
			if (this._previousParent != parent)
			{
				this.RpcChangeParent(this.ServerParentId(parent));
				this._previousParent = parent;
			}
		}
	}

	protected virtual void OnDestroy()
	{
		AdminToyBase.OnRemoved?.Invoke(this);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		this.UpdatePositionServer();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active)
		{
			this.UpdateParent();
			this.UpdatePositionClient(teleport: true);
		}
	}

	public override void OnSerialize(NetworkWriter writer, bool initialState)
	{
		base.OnSerialize(writer, initialState);
		if (initialState)
		{
			writer.WriteUInt(this.ServerParentId(base.transform.parent));
		}
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		if (initialState)
		{
			this._clientParentId = reader.ReadUInt();
		}
	}

	public virtual void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		this.SpawnerFootprint = new Footprint(admin);
		NetworkServer.Spawn(base.gameObject);
		ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{admin.LoggedNameFromRefHub()} spawned an admin toy: {this.CommandName} with NetID {base.netId}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging);
	}

	[ClientRpc]
	public void RpcChangeParent(uint parentId)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteUInt(parentId);
		this.SendRPCInternal("System.Void AdminToys.AdminToyBase::RpcChangeParent(System.UInt32)", -342419096, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private uint ServerParentId(Transform parent)
	{
		if (!(parent != null) || !parent.TryGetComponent<NetworkIdentity>(out var component))
		{
			return 0u;
		}
		return component.netId;
	}

	private void UpdateParent()
	{
		if (this._clientParentId != 0 && NetworkClient.spawned.TryGetValue(this._clientParentId, out var value))
		{
			base.transform.SetParent(value.transform, worldPositionStays: false);
		}
		else
		{
			base.transform.SetParent(null, worldPositionStays: false);
		}
	}

	private void UpdatePositionServer()
	{
		this.NetworkPosition = base.transform.localPosition;
		this.NetworkRotation = base.transform.localRotation;
		this.NetworkScale = base.transform.localScale;
	}

	protected virtual void UpdatePositionClient(bool teleport = false)
	{
		Vector3 localPosition;
		Quaternion localRotation;
		Vector3 localScale;
		if (teleport || this.MovementSmoothing == 0)
		{
			localPosition = this.Position;
			localRotation = this.Rotation;
			localScale = this.Scale;
		}
		else
		{
			float t = Time.deltaTime * (float)(int)this.MovementSmoothing * 0.3f;
			localPosition = Vector3.Lerp(base.transform.localPosition, this.Position, t);
			localRotation = Quaternion.Lerp(base.transform.localRotation, this.Rotation, t);
			localScale = Vector3.Lerp(base.transform.localScale, this.Scale, t);
		}
		base.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		base.transform.localScale = localScale;
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcChangeParent__UInt32(uint parentId)
	{
		if (!NetworkServer.active)
		{
			this._clientParentId = parentId;
			this.UpdateParent();
		}
	}

	protected static void InvokeUserCode_RpcChangeParent__UInt32(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcChangeParent called on server.");
		}
		else
		{
			((AdminToyBase)obj).UserCode_RpcChangeParent__UInt32(reader.ReadUInt());
		}
	}

	static AdminToyBase()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AdminToyBase), "System.Void AdminToys.AdminToyBase::RpcChangeParent(System.UInt32)", InvokeUserCode_RpcChangeParent__UInt32);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteVector3(this.Position);
			writer.WriteQuaternion(this.Rotation);
			writer.WriteVector3(this.Scale);
			NetworkWriterExtensions.WriteByte(writer, this.MovementSmoothing);
			writer.WriteBool(this.IsStatic);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteVector3(this.Position);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteQuaternion(this.Rotation);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteVector3(this.Scale);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this.MovementSmoothing);
		}
		if ((base.syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteBool(this.IsStatic);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Position, null, reader.ReadVector3());
			base.GeneratedSyncVarDeserialize(ref this.Rotation, null, reader.ReadQuaternion());
			base.GeneratedSyncVarDeserialize(ref this.Scale, null, reader.ReadVector3());
			base.GeneratedSyncVarDeserialize(ref this.MovementSmoothing, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this.IsStatic, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Position, null, reader.ReadVector3());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Rotation, null, reader.ReadQuaternion());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Scale, null, reader.ReadVector3());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.MovementSmoothing, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 0x10L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsStatic, null, reader.ReadBool());
		}
	}
}
