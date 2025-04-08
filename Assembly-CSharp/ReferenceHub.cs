using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CentralAuth;
using Hints;
using Interactables;
using InventorySystem;
using InventorySystem.Searching;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
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
	public static HashSet<ReferenceHub> AllHubs { get; private set; } = new HashSet<ReferenceHub>();

	[Obsolete("HostHub is obsolete, use TryGetHostHub instead.")]
	public static ReferenceHub HostHub
	{
		get
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHostHub(out referenceHub))
			{
				return null;
			}
			return referenceHub;
		}
	}

	[Obsolete("LocalHub is obsolete, use TryGetLocalHub instead.")]
	public static ReferenceHub LocalHub
	{
		get
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return null;
			}
			return referenceHub;
		}
	}

	public static int GetPlayerCount(params ClientInstanceMode[] allowedStates)
	{
		return ReferenceHub.AllHubs.Count((ReferenceHub x) => allowedStates.Contains(x.Mode));
	}

	public int PlayerId
	{
		get
		{
			return this._playerId.Value;
		}
	}

	public ClientInstanceMode Mode
	{
		get
		{
			return this.authManager.InstanceMode;
		}
	}

	public bool IsHost
	{
		get
		{
			ClientInstanceMode mode = this.Mode;
			return mode == ClientInstanceMode.Host || mode == ClientInstanceMode.DedicatedServer;
		}
	}

	public bool IsDummy
	{
		get
		{
			return this.Mode == ClientInstanceMode.Dummy;
		}
	}

	private void Awake()
	{
		ReferenceHub.AllHubs.Add(this);
		ReferenceHub.HubsByGameObjects[base.gameObject] = this;
		if (NetworkServer.active)
		{
			this.Network_playerId = new RecyclablePlayerId(true);
			this.FriendlyFireHandler = new FriendlyFireHandler(this);
		}
	}

	private void Start()
	{
		Action<ReferenceHub> onPlayerAdded = ReferenceHub.OnPlayerAdded;
		if (onPlayerAdded == null)
		{
			return;
		}
		onPlayerAdded(this);
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
		Action<ReferenceHub> onPlayerRemoved = ReferenceHub.OnPlayerRemoved;
		if (onPlayerRemoved == null)
		{
			return;
		}
		onPlayerRemoved(this);
	}

	public override string ToString()
	{
		return string.Format("{0} (Name='{1}', NetID='{2}', PlayerID='{3}')", new object[] { "ReferenceHub", base.name, base.netId, this.PlayerId });
	}

	public static ReferenceHub GetHub(GameObject player)
	{
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHub(player, out referenceHub))
		{
			return null;
		}
		return referenceHub;
	}

	public static ReferenceHub GetHub(MonoBehaviour player)
	{
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHub(player.gameObject, out referenceHub))
		{
			return null;
		}
		return referenceHub;
	}

	public static bool TryGetHub(GameObject player, out ReferenceHub hub)
	{
		if (player == null)
		{
			hub = null;
			return false;
		}
		return ReferenceHub.HubsByGameObjects.TryGetValue(player, out hub) || player.TryGetComponent<ReferenceHub>(out hub);
	}

	public static bool TryGetHub(NetworkConnection connection, out ReferenceHub hub)
	{
		NetworkIdentity identity = connection.identity;
		if (!connection.isReady || identity == null)
		{
			hub = null;
			return false;
		}
		return ReferenceHub.HubsByGameObjects.TryGetValue(identity.gameObject, out hub) || identity.TryGetComponent<ReferenceHub>(out hub);
	}

	public static bool TryGetHubNetID(uint netId, out ReferenceHub hub)
	{
		NetworkIdentity networkIdentity;
		if (NetworkUtils.SpawnedNetIds.TryGetValue(netId, out networkIdentity) && ReferenceHub.TryGetHub(networkIdentity.gameObject, out hub))
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
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (referenceHub.isLocalPlayer)
			{
				hub = referenceHub;
				ReferenceHub._localHub = referenceHub;
				ReferenceHub._localHubSet = true;
				return true;
			}
		}
		hub = null;
		return false;
	}

	public static bool TryGetPovHub(out ReferenceHub hub)
	{
		ReferenceHub referenceHub;
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub))
		{
			hub = referenceHub;
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
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (referenceHub.IsHost)
			{
				hub = referenceHub;
				ReferenceHub._hostHub = referenceHub;
				ReferenceHub._hostHubSet = true;
				return true;
			}
		}
		hub = null;
		return false;
	}

	public static ReferenceHub GetHub(int playerId)
	{
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHub(playerId, out referenceHub))
		{
			return null;
		}
		return referenceHub;
	}

	public static bool TryGetHub(int playerId, out ReferenceHub hub)
	{
		if (playerId > 0)
		{
			if (ReferenceHub.HubByPlayerIds.TryGetValue(playerId, out hub))
			{
				return true;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.PlayerId == playerId)
				{
					ReferenceHub.HubByPlayerIds[playerId] = referenceHub;
					hub = referenceHub;
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
		ReferenceHub referenceHub = obj as ReferenceHub;
		return referenceHub != null && this == referenceHub;
	}

	public override int GetHashCode()
	{
		return base.gameObject.GetHashCode();
	}

	public static bool operator ==(ReferenceHub left, ReferenceHub right)
	{
		return left == right;
	}

	public static bool operator !=(ReferenceHub left, ReferenceHub right)
	{
		return left != right;
	}

	public override bool Weaved()
	{
		return true;
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
			base.GeneratedSyncVarSetter<RecyclablePlayerId>(value, ref this._playerId, 1UL, null);
		}
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
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteRecyclablePlayerId(this._playerId);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<RecyclablePlayerId>(ref this._playerId, null, reader.ReadRecyclablePlayerId());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<RecyclablePlayerId>(ref this._playerId, null, reader.ReadRecyclablePlayerId());
		}
	}

	public static Action<ReferenceHub> OnPlayerAdded;

	public static Action<ReferenceHub> OnPlayerRemoved;

	private static readonly Dictionary<GameObject, ReferenceHub> HubsByGameObjects = new Dictionary<GameObject, ReferenceHub>(20, new ReferenceHub.GameObjectComparer());

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

	public PlayerInteract playerInteract;

	public InteractionCoordinator interCoordinator;

	public PlayerEffectsController playerEffectsController;

	public HintDisplay hints;

	public AspectRatioSync aspectRatioSync;

	public PlayerRateLimitHandler playerRateLimitHandler;

	public GameConsoleTransmission gameConsoleTransmission;

	public PlayerAuthenticationManager authManager;

	public EncryptedChannelManager encryptedChannelManager;

	internal FriendlyFireHandler FriendlyFireHandler;

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
}
