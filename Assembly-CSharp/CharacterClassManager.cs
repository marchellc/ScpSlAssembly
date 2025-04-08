using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CentralAuth;
using GameCore;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using Mirror.RemoteCalls;
using PlayerStatsSystem;
using Security;
using ServerOutput;
using UnityEngine;

public class CharacterClassManager : NetworkBehaviour
{
	public bool GodMode
	{
		get
		{
			return this._godMode;
		}
		set
		{
			this._godMode = value;
			this._hub.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.GodMode, value);
		}
	}

	public static event Action OnRoundStarted;

	public static event Action ServerOnRoundStartTriggered;

	public static event Action<ushort> OnMaxPlayersChanged;

	private void Start()
	{
		this._hub = ReferenceHub.GetHub(this);
		if (!NetworkServer.active)
		{
			return;
		}
		this._commandRateLimit = this._hub.playerRateLimitHandler.RateLimits[1];
		if (!base.isLocalPlayer)
		{
			return;
		}
		ServerLogs.StartLogging();
		FriendlyFireConfig.PauseDetector = false;
		CustomLiteNetLib4MirrorTransport.DelayConnections = false;
		IdleMode.PauseIdleMode = false;
		ServerConsole.AddOutputEntry(default(RoundRestartedEntry));
		this.NetworkPastebin = ConfigFile.ServerConfig.GetString("serverinfo_pastebin_id", "");
		if (ServerStatic.IsDedicated)
		{
			ServerEvents.OnWaitingForPlayers();
			ServerConsole.AddLog("Waiting for players...", ConsoleColor.Gray, false);
		}
		base.StartCoroutine(this.Init());
	}

	private void Update()
	{
		if (base.isLocalPlayer && NetworkServer.active && this.MaxPlayers != (ushort)CustomNetworkManager.slots)
		{
			this.NetworkMaxPlayers = (ushort)CustomNetworkManager.slots;
		}
	}

	private void MaxPlayersHook(ushort prev, ushort cur)
	{
		if (prev == cur)
		{
			return;
		}
		Action<ushort> onMaxPlayersChanged = CharacterClassManager.OnMaxPlayersChanged;
		if (onMaxPlayersChanged == null)
		{
			return;
		}
		onMaxPlayersChanged(cur);
	}

	public void SyncServerCmdBinding()
	{
		if (!base.isServer || !CharacterClassManager.EnableSyncServerCmdBinding)
		{
			return;
		}
		foreach (CmdBinding.Bind bind in CmdBinding.Bindings)
		{
			if (bind.command.StartsWith(".") || bind.command.StartsWith("/"))
			{
				this.TargetChangeCmdBinding(bind.key, bind.command);
			}
		}
	}

	[TargetRpc]
	public void TargetChangeCmdBinding(KeyCode code, string cmd)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		global::Mirror.GeneratedNetworkCode._Write_UnityEngine.KeyCode(networkWriterPooled, code);
		networkWriterPooled.WriteString(cmd);
		this.SendTargetRPCInternal(null, "System.Void CharacterClassManager::TargetChangeCmdBinding(UnityEngine.KeyCode,System.String)", -1033122126, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private IEnumerator Init()
	{
		if (NonFacilityCompatibility.currentSceneSettings.roundAutostart)
		{
			CharacterClassManager.ForceRoundStart();
		}
		else
		{
			short originalTimeLeft = ConfigFile.ServerConfig.GetShort("lobby_waiting_time", 20);
			short timeLeft = originalTimeLeft;
			int topPlayers = 2;
			while (RoundStart.singleton.Timer != -1)
			{
				if (timeLeft == -2)
				{
					timeLeft = originalTimeLeft;
				}
				int playerCount = ReferenceHub.GetPlayerCount(new ClientInstanceMode[]
				{
					ClientInstanceMode.ReadyClient,
					ClientInstanceMode.Host,
					ClientInstanceMode.Dummy
				});
				if (!RoundStart.LobbyLock && playerCount > 1)
				{
					if (playerCount > topPlayers)
					{
						topPlayers = playerCount;
						if (timeLeft < originalTimeLeft)
						{
							do
							{
								short num = timeLeft;
								timeLeft = num + 1;
								if (timeLeft % 5 != 0)
								{
									break;
								}
							}
							while (timeLeft < originalTimeLeft);
						}
					}
					else
					{
						short num = timeLeft;
						timeLeft = num - 1;
					}
					if (playerCount >= ((CustomNetworkManager)NetworkManager.singleton).ReservedMaxPlayers)
					{
						timeLeft = -1;
					}
					if (timeLeft == -1)
					{
						CharacterClassManager.ForceRoundStart();
					}
				}
				else
				{
					timeLeft = -2;
				}
				if (RoundStart.singleton.Timer != -1)
				{
					RoundStart.singleton.NetworkTimer = timeLeft;
				}
				yield return new WaitForSeconds(1f);
			}
		}
		ServerEvents.OnRoundStarted();
		this.NetworkRoundStarted = true;
		Action serverOnRoundStartTriggered = CharacterClassManager.ServerOnRoundStartTriggered;
		if (serverOnRoundStartTriggered != null)
		{
			serverOnRoundStartTriggered();
		}
		this.RpcRoundStarted();
		yield break;
	}

	public static bool ForceRoundStart()
	{
		if (!NetworkServer.active)
		{
			return false;
		}
		RoundStartingEventArgs roundStartingEventArgs = new RoundStartingEventArgs();
		ServerEvents.OnRoundStarting(roundStartingEventArgs);
		if (!roundStartingEventArgs.IsAllowed)
		{
			return false;
		}
		ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Round has been started.", ServerLogs.ServerLogType.GameEvent, false);
		ServerConsole.AddLog("New round has been started.", ConsoleColor.Gray, false);
		RoundStart.singleton.NetworkTimer = -1;
		RoundStart.RoundStartTimer.Restart();
		return true;
	}

	[TargetRpc]
	private void TargetSetDisconnectError(NetworkConnection conn, string message)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(message);
		this.SendTargetRPCInternal(conn, "System.Void CharacterClassManager::TargetSetDisconnectError(Mirror.NetworkConnection,System.String)", -2106075371, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[Command(channel = 4)]
	private void CmdConfirmDisconnect()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		base.SendCommandInternal("System.Void CharacterClassManager::CmdConfirmDisconnect()", 460932189, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public void DisconnectClient(NetworkConnection conn, string message)
	{
		this.TargetSetDisconnectError(conn, message);
		Timing.RunCoroutine(CharacterClassManager._DisconnectAfterTimeout(conn), Segment.FixedUpdate);
	}

	private static IEnumerator<float> _DisconnectAfterTimeout(NetworkConnection conn)
	{
		int num;
		for (int i = 0; i < 150; i = num + 1)
		{
			yield return 0f;
			num = i;
		}
		if (conn != null)
		{
			conn.Disconnect();
		}
		yield break;
	}

	[ClientRpc]
	private void RpcRoundStarted()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void CharacterClassManager::RpcRoundStarted()", -2093950497, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	static CharacterClassManager()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(CharacterClassManager), "System.Void CharacterClassManager::CmdConfirmDisconnect()", new RemoteCallDelegate(CharacterClassManager.InvokeUserCode_CmdConfirmDisconnect), true);
		RemoteProcedureCalls.RegisterRpc(typeof(CharacterClassManager), "System.Void CharacterClassManager::RpcRoundStarted()", new RemoteCallDelegate(CharacterClassManager.InvokeUserCode_RpcRoundStarted));
		RemoteProcedureCalls.RegisterRpc(typeof(CharacterClassManager), "System.Void CharacterClassManager::TargetChangeCmdBinding(UnityEngine.KeyCode,System.String)", new RemoteCallDelegate(CharacterClassManager.InvokeUserCode_TargetChangeCmdBinding__KeyCode__String));
		RemoteProcedureCalls.RegisterRpc(typeof(CharacterClassManager), "System.Void CharacterClassManager::TargetSetDisconnectError(Mirror.NetworkConnection,System.String)", new RemoteCallDelegate(CharacterClassManager.InvokeUserCode_TargetSetDisconnectError__NetworkConnection__String));
	}

	public override bool Weaved()
	{
		return true;
	}

	public string NetworkPastebin
	{
		get
		{
			return this.Pastebin;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.Pastebin, 1UL, null);
		}
	}

	public ushort NetworkMaxPlayers
	{
		get
		{
			return this.MaxPlayers;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<ushort>(value, ref this.MaxPlayers, 2UL, new Action<ushort, ushort>(this.MaxPlayersHook));
		}
	}

	public bool NetworkRoundStarted
	{
		get
		{
			return this.RoundStarted;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.RoundStarted, 4UL, null);
		}
	}

	protected void UserCode_TargetChangeCmdBinding__KeyCode__String(KeyCode code, string cmd)
	{
	}

	protected static void InvokeUserCode_TargetChangeCmdBinding__KeyCode__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetChangeCmdBinding called on server.");
			return;
		}
		((CharacterClassManager)obj).UserCode_TargetChangeCmdBinding__KeyCode__String(global::Mirror.GeneratedNetworkCode._Read_UnityEngine.KeyCode(reader), reader.ReadString());
	}

	protected void UserCode_TargetSetDisconnectError__NetworkConnection__String(NetworkConnection conn, string message)
	{
		((CustomNetworkManager)LiteNetLib4MirrorNetworkManager.singleton).disconnectMessage = message;
		this.CmdConfirmDisconnect();
	}

	protected static void InvokeUserCode_TargetSetDisconnectError__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetDisconnectError called on server.");
			return;
		}
		((CharacterClassManager)obj).UserCode_TargetSetDisconnectError__NetworkConnection__String(null, reader.ReadString());
	}

	protected void UserCode_CmdConfirmDisconnect()
	{
		NetworkConnectionToClient connectionToClient = base.connectionToClient;
		if (connectionToClient == null)
		{
			return;
		}
		connectionToClient.Disconnect();
	}

	protected static void InvokeUserCode_CmdConfirmDisconnect(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdConfirmDisconnect called on client.");
			return;
		}
		((CharacterClassManager)obj).UserCode_CmdConfirmDisconnect();
	}

	protected void UserCode_RpcRoundStarted()
	{
		Action onRoundStarted = CharacterClassManager.OnRoundStarted;
		if (onRoundStarted == null)
		{
			return;
		}
		onRoundStarted();
	}

	protected static void InvokeUserCode_RpcRoundStarted(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRoundStarted called on server.");
			return;
		}
		((CharacterClassManager)obj).UserCode_RpcRoundStarted();
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteString(this.Pastebin);
			writer.WriteUShort(this.MaxPlayers);
			writer.WriteBool(this.RoundStarted);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteString(this.Pastebin);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteUShort(this.MaxPlayers);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this.RoundStarted);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.Pastebin, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize<ushort>(ref this.MaxPlayers, new Action<ushort, ushort>(this.MaxPlayersHook), reader.ReadUShort());
			base.GeneratedSyncVarDeserialize<bool>(ref this.RoundStarted, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.Pastebin, null, reader.ReadString());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<ushort>(ref this.MaxPlayers, new Action<ushort, ushort>(this.MaxPlayersHook), reader.ReadUShort());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.RoundStarted, null, reader.ReadBool());
		}
	}

	private ReferenceHub _hub;

	[NonSerialized]
	private bool _godMode;

	private bool _wasAnytimeAlive;

	internal static bool EnableSyncServerCmdBinding;

	[SyncVar]
	public string Pastebin;

	[SyncVar(hook = "MaxPlayersHook")]
	public ushort MaxPlayers;

	internal static bool CuffedChangeTeam = true;

	[SyncVar]
	public bool RoundStarted;

	private RateLimit _commandRateLimit;
}
