using System;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using Footprinting;
using InventorySystem.Searching;
using Mirror;
using Mirror.RemoteCalls;
using RoundRestarting;
using UnityEngine;

namespace InventorySystem.Items.Pickups
{
	[RequireComponent(typeof(Rigidbody))]
	public abstract class ItemPickupBase : NetworkBehaviour, IIdentifierProvider
	{
		public static event Action<ItemPickupBase> OnPickupAdded;

		public static event Action<ItemPickupBase> OnPickupDestroyed;

		public static event Action<ItemPickupBase> OnBeforePickupDestroyed;

		public event Action OnInfoChanged;

		public event Action<PickupSyncInfo, PickupSyncInfo> OnInfoChangedPrevNew;

		protected virtual PickupPhysicsModule DefaultPhysicsModule
		{
			get
			{
				return new PickupStandardPhysics(this, PickupStandardPhysics.FreezingMode.Default);
			}
		}

		public PickupPhysicsModule PhysicsModule { get; protected set; }

		public ItemIdentifier ItemId
		{
			get
			{
				return new ItemIdentifier(this.Info.ItemId, this.Info.Serial);
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

		private void InfoReceivedHook(PickupSyncInfo oldInfo, PickupSyncInfo newInfo)
		{
			Action onInfoChanged = this.OnInfoChanged;
			if (onInfoChanged != null)
			{
				onInfoChanged();
			}
			Action<PickupSyncInfo, PickupSyncInfo> onInfoChangedPrevNew = this.OnInfoChangedPrevNew;
			if (onInfoChangedPrevNew == null)
			{
				return;
			}
			onInfoChangedPrevNew(oldInfo, newInfo);
		}

		public virtual float SearchTimeForPlayer(ReferenceHub hub)
		{
			float num = 0.245f + 0.175f * this.Info.WeightKg;
			foreach (StatusEffectBase statusEffectBase in hub.playerEffectsController.AllEffects)
			{
				if (statusEffectBase.IsEnabled)
				{
					ISearchTimeModifier searchTimeModifier = statusEffectBase as ISearchTimeModifier;
					if (searchTimeModifier != null)
					{
						num = searchTimeModifier.ProcessSearchTime(num);
					}
				}
			}
			ISearchTimeModifier searchTimeModifier2 = hub.inventory.CurInstance as ISearchTimeModifier;
			if (searchTimeModifier2 != null && searchTimeModifier2 != null)
			{
				num = searchTimeModifier2.ProcessSearchTime(num);
			}
			return num;
		}

		[Server]
		public void DestroySelf()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void InventorySystem.Items.Pickups.ItemPickupBase::DestroySelf()' called when server was not active");
				return;
			}
			NetworkServer.Destroy(base.gameObject);
		}

		public override void OnStopClient()
		{
			base.OnStopClient();
			Action<ItemPickupBase> onBeforePickupDestroyed = ItemPickupBase.OnBeforePickupDestroyed;
			if (onBeforePickupDestroyed == null)
			{
				return;
			}
			onBeforePickupDestroyed(this);
		}

		[ClientRpc]
		internal virtual void SendPhysicsModuleRpc(ArraySegment<byte> arrSeg)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteArraySegmentAndSize(arrSeg);
			this.SendRPCInternal("System.Void InventorySystem.Items.Pickups.ItemPickupBase::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", 254399230, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		protected virtual void Awake()
		{
			this.PhysicsModule = this.DefaultPhysicsModule;
		}

		protected virtual void Start()
		{
			Action<ItemPickupBase> onPickupAdded = ItemPickupBase.OnPickupAdded;
			if (onPickupAdded != null)
			{
				onPickupAdded(this);
			}
			if (!NetworkServer.active)
			{
				return;
			}
			this._wasServer = true;
			RoundRestart.OnRestartTriggered += this.DestroySelf;
		}

		protected virtual void OnDestroy()
		{
			PickupPhysicsModule physicsModule = this.PhysicsModule;
			if (physicsModule != null)
			{
				physicsModule.DestroyModule();
			}
			Action<ItemPickupBase> onPickupDestroyed = ItemPickupBase.OnPickupDestroyed;
			if (onPickupDestroyed != null)
			{
				onPickupDestroyed(this);
			}
			if (!this._wasServer)
			{
				return;
			}
			RoundRestart.OnRestartTriggered -= this.DestroySelf;
		}

		protected ItemPickupBase()
		{
			base.InitSyncObject(this.PhysicsModuleSyncData);
		}

		public override bool Weaved()
		{
			return true;
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
				base.GeneratedSyncVarSetter<PickupSyncInfo>(value, ref this.Info, 1UL, new Action<PickupSyncInfo, PickupSyncInfo>(this.InfoReceivedHook));
			}
		}

		protected virtual void UserCode_SendPhysicsModuleRpc__ArraySegment`1(ArraySegment<byte> arrSeg)
		{
			using (NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(arrSeg))
			{
				PickupPhysicsModule physicsModule = this.PhysicsModule;
				if (physicsModule != null)
				{
					physicsModule.ClientProcessRpc(networkReaderPooled);
				}
			}
		}

		protected static void InvokeUserCode_SendPhysicsModuleRpc__ArraySegment`1(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC SendPhysicsModuleRpc called on server.");
				return;
			}
			((ItemPickupBase)obj).UserCode_SendPhysicsModuleRpc__ArraySegment`1(reader.ReadArraySegmentAndSize());
		}

		static ItemPickupBase()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(ItemPickupBase), "System.Void InventorySystem.Items.Pickups.ItemPickupBase::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", new RemoteCallDelegate(ItemPickupBase.InvokeUserCode_SendPhysicsModuleRpc__ArraySegment`1));
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
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WritePickupSyncInfo(this.Info);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<PickupSyncInfo>(ref this.Info, new Action<PickupSyncInfo, PickupSyncInfo>(this.InfoReceivedHook), reader.ReadPickupSyncInfo());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<PickupSyncInfo>(ref this.Info, new Action<PickupSyncInfo, PickupSyncInfo>(this.InfoReceivedHook), reader.ReadPickupSyncInfo());
			}
		}

		private const float MinimalPickupTime = 0.245f;

		private const float WeightToTime = 0.175f;

		[SyncVar(hook = "InfoReceivedHook")]
		public PickupSyncInfo Info = PickupSyncInfo.None;

		public SyncList<byte> PhysicsModuleSyncData = new SyncList<byte>();

		public Footprint PreviousOwner;

		private Transform _transform;

		private bool _transformCacheSet;

		private bool _wasServer;
	}
}
