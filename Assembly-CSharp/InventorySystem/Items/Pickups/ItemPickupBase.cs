using System;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using Footprinting;
using InventorySystem.Searching;
using Mirror;
using Mirror.RemoteCalls;
using RoundRestarting;
using UnityEngine;

namespace InventorySystem.Items.Pickups;

[RequireComponent(typeof(Rigidbody))]
public abstract class ItemPickupBase : NetworkBehaviour, IIdentifierProvider, ISearchable
{
	private const float MinimalPickupTime = 0.245f;

	private const float WeightToTime = 0.175f;

	[SyncVar(hook = "InfoReceivedHook")]
	public PickupSyncInfo Info = PickupSyncInfo.None;

	public SyncList<byte> PhysicsModuleSyncData = new SyncList<byte>();

	public Footprint PreviousOwner;

	private Transform _transform;

	private bool _transformCacheSet;

	private bool _wasServer;

	protected virtual PickupPhysicsModule DefaultPhysicsModule => new PickupStandardPhysics(this);

	public PickupPhysicsModule PhysicsModule { get; protected set; }

	public ItemIdentifier ItemId => new ItemIdentifier(this.Info.ItemId, this.Info.Serial);

	public bool CanSearch
	{
		get
		{
			if (!this.Info.Locked)
			{
				return !this.Info.InUse;
			}
			return false;
		}
	}

	public Vector3 Position
	{
		get
		{
			return this.CachedTransform.position;
		}
		set
		{
			this.CachedTransform.position = value;
		}
	}

	public Quaternion Rotation
	{
		get
		{
			return this.CachedTransform.rotation;
		}
		set
		{
			this.CachedTransform.rotation = value;
		}
	}

	private Transform CachedTransform
	{
		get
		{
			if (!this._transformCacheSet)
			{
				this._transformCacheSet = true;
				this._transform = base.transform;
			}
			return this._transform;
		}
	}

	public PickupSyncInfo NetworkInfo
	{
		get
		{
			return this.Info;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Info, 1uL, InfoReceivedHook);
		}
	}

	public static event Action<ItemPickupBase> OnPickupAdded;

	public static event Action<ItemPickupBase> OnPickupDestroyed;

	public static event Action<ItemPickupBase> OnBeforePickupDestroyed;

	public event Action OnInfoChanged;

	public event Action<PickupSyncInfo, PickupSyncInfo> OnInfoChangedPrevNew;

	private void InfoReceivedHook(PickupSyncInfo oldInfo, PickupSyncInfo newInfo)
	{
		this.OnInfoChanged?.Invoke();
		this.OnInfoChangedPrevNew?.Invoke(oldInfo, newInfo);
	}

	public virtual float SearchTimeForPlayer(ReferenceHub hub)
	{
		float num = 0.245f + 0.175f * this.Info.WeightKg;
		StatusEffectBase[] allEffects = hub.playerEffectsController.AllEffects;
		foreach (StatusEffectBase statusEffectBase in allEffects)
		{
			if (statusEffectBase.IsEnabled && statusEffectBase is ISearchTimeModifier searchTimeModifier)
			{
				num = searchTimeModifier.ProcessSearchTime(num);
			}
		}
		if (hub.inventory.CurInstance is ISearchTimeModifier searchTimeModifier2 && searchTimeModifier2 != null)
		{
			num = searchTimeModifier2.ProcessSearchTime(num);
		}
		return num;
	}

	public ISearchCompletor GetSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		return this.GetPickupSearchCompletor(coordinator, sqrDistance);
	}

	public virtual PickupSearchCompletor GetPickupSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		if (!InventoryItemLoader.TryGetItem<ItemBase>(this.Info.ItemId, out var result))
		{
			return null;
		}
		return new ItemSearchCompletor(coordinator.Hub, this, result, sqrDistance);
	}

	public bool ServerValidateRequest(NetworkConnection source, SearchSessionPipe session)
	{
		if (this.Info.Locked)
		{
			session.Invalidate();
			source?.identity.GetComponent<GameConsoleTransmission>().SendToClient("Pickup request rejected - target is locked.", "red");
			return false;
		}
		if (this.Info.InUse)
		{
			session.Invalidate();
			source?.identity.GetComponent<GameConsoleTransmission>().SendToClient("Pickup request rejected - target is in use.", "red");
			return false;
		}
		PickupSyncInfo info = this.Info;
		info.InUse = true;
		this.NetworkInfo = info;
		return true;
	}

	public void ServerHandleAbort(ReferenceHub hub)
	{
		PickupSyncInfo info = this.Info;
		info.InUse = false;
		this.NetworkInfo = info;
	}

	[Server]
	public void DestroySelf()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void InventorySystem.Items.Pickups.ItemPickupBase::DestroySelf()' called when server was not active");
		}
		else
		{
			NetworkServer.Destroy(base.gameObject);
		}
	}

	[ClientRpc]
	internal virtual void SendPhysicsModuleRpc(ArraySegment<byte> arrSeg)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteArraySegmentAndSize(arrSeg);
		this.SendRPCInternal("System.Void InventorySystem.Items.Pickups.ItemPickupBase::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", 254399230, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	protected virtual void Awake()
	{
		this.PhysicsModule = this.DefaultPhysicsModule;
	}

	protected virtual void Start()
	{
		ItemPickupBase.OnPickupAdded?.Invoke(this);
		if (NetworkServer.active)
		{
			this._wasServer = true;
			RoundRestart.OnRestartTriggered += DestroySelf;
		}
	}

	protected virtual void OnDestroy()
	{
		this.PhysicsModule?.DestroyModule();
		ItemPickupBase.OnPickupDestroyed?.Invoke(this);
		if (this._wasServer)
		{
			RoundRestart.OnRestartTriggered -= DestroySelf;
		}
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		ItemPickupBase.OnBeforePickupDestroyed?.Invoke(this);
	}

	protected ItemPickupBase()
	{
		base.InitSyncObject(this.PhysicsModuleSyncData);
	}

	NetworkIdentity ISearchable.get_netIdentity()
	{
		return base.netIdentity;
	}

	public override bool Weaved()
	{
		return true;
	}

	protected virtual void UserCode_SendPhysicsModuleRpc__ArraySegment_00601(ArraySegment<byte> arrSeg)
	{
		using NetworkReaderPooled rpcData = NetworkReaderPool.Get(arrSeg);
		this.PhysicsModule?.ClientProcessRpc(rpcData);
	}

	protected static void InvokeUserCode_SendPhysicsModuleRpc__ArraySegment_00601(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC SendPhysicsModuleRpc called on server.");
		}
		else
		{
			((ItemPickupBase)obj).UserCode_SendPhysicsModuleRpc__ArraySegment_00601(reader.ReadArraySegmentAndSize());
		}
	}

	static ItemPickupBase()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(ItemPickupBase), "System.Void InventorySystem.Items.Pickups.ItemPickupBase::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", InvokeUserCode_SendPhysicsModuleRpc__ArraySegment_00601);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WritePickupSyncInfo(this.Info);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WritePickupSyncInfo(this.Info);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, InfoReceivedHook, reader.ReadPickupSyncInfo());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, InfoReceivedHook, reader.ReadPickupSyncInfo());
		}
	}
}
