using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CentralAuth;
using Hints;
using Interactables;
using InventorySystem;
using InventorySystem.Searching;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using RemoteAdmin;
using Security;
using UnityEngine;
using Utils.Networking;

public sealed class ReferenceHub : NetworkBehaviour, IEquatable<ReferenceHub>
{
	private class GameObjectComparer : EqualityComparer<GameObject>
	{
		public override bool Equals(GameObject x, GameObject y)
		{
			return x == y;
		}

		public override int GetHashCode(GameObject obj)
		{
			if (!(obj == null))
			{
				return obj.GetHashCode();
			}
			return 0;
		}
	}

	private static readonly Dictionary<GameObject, ReferenceHub> HubsByGameObjects = new Dictionary<GameObject, ReferenceHub>(20, new GameObjectComparer());

	private static readonly Dictionary<int, ReferenceHub> HubByPlayerIds = new Dictionary<int, ReferenceHub>(20);

	private static bool _localHubSet;

	private static bool _hostHubSet;

	private static ReferenceHub _localHub;

	private static ReferenceHub _hostHub;

	[SyncVar]
	private RecyclablePlayerId _playerId;

	public Transform PlayerCameraReference;

	public NetworkIdentity networkIdentity;

	public CharacterClassManager characterClassManager;

	public PlayerRoleManager roleManager;

	public PlayerStats playerStats;

	public Inventory inventory;

	public SearchCoordinator searchCoordinator;

	public ServerRoles serverRoles;

	public QueryProcessor queryProcessor;

	public NicknameSync nicknameSync;

	public InteractionCoordinator interCoordinator;

	public PlayerEffectsController playerEffectsController;

	public HintDisplay hints;

	public AspectRatioSync aspectRatioSync;

	public PlayerRateLimitHandler playerRateLimitHandler;

	public GameConsoleTransmission gameConsoleTransmission;

	public PlayerAuthenticationManager authManager;

	public EncryptedChannelManager encryptedChannelManager;

	public CurrentRoomPlayerCache CurrentRoomPlayerCache;

	internal FriendlyFireHandler FriendlyFireHandler;

	public static HashSet<ReferenceHub> AllHubs { get; private set; } = new HashSet<ReferenceHub>();

	[Obsolete("HostHub is obsolete, use TryGetHostHub instead.")]
	public static ReferenceHub HostHub
	{
		get
		{
			if (!ReferenceHub.TryGetHostHub(out var hub))
			{
				return null;
			}
			return hub;
		}
	}

	[Obsolete("LocalHub is obsolete, use TryGetLocalHub instead.")]
	public static ReferenceHub LocalHub
	{
		get
		{
			if (!ReferenceHub.TryGetLocalHub(out var hub))
			{
				return null;
			}
			return hub;
		}
	}

	public int PlayerId => this._playerId.Value;

	public ClientInstanceMode Mode => this.authManager.InstanceMode;

	public bool IsHost
	{
		get
		{
			ClientInstanceMode mode = this.Mode;
			return mode == ClientInstanceMode.Host || mode == ClientInstanceMode.DedicatedServer;
		}
	}

	public bool IsDummy => this.Mode == ClientInstanceMode.Dummy;

	public bool IsPOV
	{
		get
		{
			if (!base.isLocalPlayer)
			{
				return this.IsLocallySpectated();
			}
			return true;
		}
	}

	public RecyclablePlayerId Network_playerId
	{
		get
		{
			return this._playerId;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._playerId, 1uL, null);
		}
	}

	public static event Action<ReferenceHub> OnPlayerAdded;

	public static event Action<ReferenceHub> OnPlayerRemoved;

	public static event Action<ReferenceHub> OnBeforePlayerDestroyed;

	public static int GetPlayerCount(ClientInstanceMode allowedState)
	{
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allowedState == allHub.Mode)
			{
				num++;
			}
		}
		return num;
	}

	public static int GetPlayerCount(ClientInstanceMode allowedState, ClientInstanceMode allowedState2)
	{
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allowedState == allHub.Mode || allowedState2 == allHub.Mode)
			{
				num++;
			}
		}
		return num;
	}

	public static int GetPlayerCount(ClientInstanceMode allowedState, ClientInstanceMode allowedState2, ClientInstanceMode allowedState3)
	{
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allowedState == allHub.Mode || allowedState2 == allHub.Mode || allowedState3 == allHub.Mode)
			{
				num++;
			}
		}
		return num;
	}

	private void Awake()
	{
		ReferenceHub.AllHubs.Add(this);
		ReferenceHub.HubsByGameObjects[base.gameObject] = this;
		if (NetworkServer.active)
		{
			this.Network_playerId = new RecyclablePlayerId(useMinQueue: true);
			this.FriendlyFireHandler = new FriendlyFireHandler(this);
		}
	}

	private void Start()
	{
		ReferenceHub.OnPlayerAdded?.Invoke(this);
	}

	private void OnDestroy()
	{
		if (!base.isLocalPlayer)
		{
			PlayerEvents.OnLeft(new PlayerLeftEventArgs(this));
		}
		ReferenceHub.AllHubs.Remove(this);
		ReferenceHub.HubsByGameObjects.Remove(base.gameObject);
		ReferenceHub.HubByPlayerIds.Remove(this.PlayerId);
		this._playerId.Destroy();
		if (ReferenceHub._hostHub == this)
		{
			ReferenceHub._hostHub = null;
			ReferenceHub._hostHubSet = false;
		}
		if (ReferenceHub._localHub == this)
		{
			ReferenceHub._localHub = null;
			ReferenceHub._localHubSet = false;
		}
		ReferenceHub.OnPlayerRemoved?.Invoke(this);
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		ReferenceHub.OnBeforePlayerDestroyed?.Invoke(this);
	}

	public override string ToString()
	{
		return string.Format("{0} (Name='{1}', NetID='{2}', PlayerID='{3}')", "ReferenceHub", base.name, base.netId, this.PlayerId);
	}

	public static ReferenceHub GetHub(GameObject player)
	{
		if (!ReferenceHub.TryGetHub(player, out var hub))
		{
			return null;
		}
		return hub;
	}

	public static ReferenceHub GetHub(MonoBehaviour player)
	{
		if (!ReferenceHub.TryGetHub(player.gameObject, out var hub))
		{
			return null;
		}
		return hub;
	}

	public static bool TryGetHub(GameObject player, out ReferenceHub hub)
	{
		if (player == null)
		{
			hub = null;
			return false;
		}
		if (!ReferenceHub.HubsByGameObjects.TryGetValue(player, out hub))
		{
			return player.TryGetComponent<ReferenceHub>(out hub);
		}
		return true;
	}

	public static bool TryGetHub(NetworkConnection connection, out ReferenceHub hub)
	{
		NetworkIdentity identity = connection.identity;
		if (!connection.isReady || identity == null)
		{
			hub = null;
			return false;
		}
		if (!ReferenceHub.HubsByGameObjects.TryGetValue(identity.gameObject, out hub))
		{
			return identity.TryGetComponent<ReferenceHub>(out hub);
		}
		return true;
	}

	public static bool TryGetHubNetID(uint netId, out ReferenceHub hub)
	{
		if (NetworkUtils.SpawnedNetIds.TryGetValue(netId, out var value) && ReferenceHub.TryGetHub(value.gameObject, out hub))
		{
			return true;
		}
		hub = null;
		return false;
	}

	public static bool TryGetLocalHub(out ReferenceHub hub)
	{
		if (ReferenceHub._localHubSet)
		{
			hub = ReferenceHub._localHub;
			return true;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.isLocalPlayer)
			{
				hub = allHub;
				ReferenceHub._localHub = allHub;
				ReferenceHub._localHubSet = true;
				return true;
			}
		}
		hub = null;
		return false;
	}

	public static bool TryGetPovHub(out ReferenceHub hub)
	{
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out var hub2))
		{
			hub = hub2;
			return true;
		}
		return ReferenceHub.TryGetLocalHub(out hub);
	}

	public static bool TryGetHostHub(out ReferenceHub hub)
	{
		if (ReferenceHub._hostHubSet)
		{
			hub = ReferenceHub._hostHub;
			return true;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.IsHost)
			{
				hub = allHub;
				ReferenceHub._hostHub = allHub;
				ReferenceHub._hostHubSet = true;
				return true;
			}
		}
		hub = null;
		return false;
	}

	public static ReferenceHub GetHub(int playerId)
	{
		if (!ReferenceHub.TryGetHub(playerId, out var hub))
		{
			return null;
		}
		return hub;
	}

	public static bool TryGetHub(int playerId, out ReferenceHub hub)
	{
		if (playerId > 0)
		{
			if (ReferenceHub.HubByPlayerIds.TryGetValue(playerId, out hub))
			{
				return true;
			}
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.PlayerId == playerId)
				{
					ReferenceHub.HubByPlayerIds[playerId] = allHub;
					hub = allHub;
					return true;
				}
			}
		}
		hub = null;
		return false;
	}

	public bool Equals(ReferenceHub other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		if (obj is ReferenceHub referenceHub)
		{
			return this == referenceHub;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.gameObject.GetHashCode();
	}

	public static bool operator ==(ReferenceHub left, ReferenceHub right)
	{
		return (UnityEngine.Object)left == (UnityEngine.Object)right;
	}

	public static bool operator !=(ReferenceHub left, ReferenceHub right)
	{
		return (UnityEngine.Object)left != (UnityEngine.Object)right;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteRecyclablePlayerId(this._playerId);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteRecyclablePlayerId(this._playerId);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._playerId, null, reader.ReadRecyclablePlayerId());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._playerId, null, reader.ReadRecyclablePlayerId());
		}
	}
}
