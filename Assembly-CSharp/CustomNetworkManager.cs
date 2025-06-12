using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CentralAuth;
using GameCore;
using MEC;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using Query;
using Steam;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Utils.NonAllocLINQ;

public class CustomNetworkManager : LiteNetLib4MirrorNetworkManager
{
	[Serializable]
	public class DisconnectLog
	{
		[Serializable]
		public class LogButton
		{
			public ConnInfoButton[] actions;
		}

		[Multiline]
		public string msg_en;

		public LogButton button;

		public bool autoHideOnSceneLoad;
	}

	public static readonly HashSet<Func<CustomNetworkManager, bool>> TryStartClientChecks = new HashSet<Func<CustomNetworkManager, bool>>();

	[SerializeField]
	private GameObject popup;

	[SerializeField]
	private GameObject createPopForce;

	[SerializeField]
	private GameObject loadingpop;

	public GameObject createpop;

	public RectTransform contSize;

	internal static QueryServer QueryServer;

	public DisconnectLog[] logs;

	private int _curLogId;

	internal static bool reconnecting;

	internal static float reconnectTime;

	internal static float triggerReconnectTime;

	private bool _queryEnabled;

	private bool _configLoaded;

	private bool _activated;

	private float _dictCleanupTime;

	private float _ipRateLimitTime;

	private float _userIdRateLimitTime;

	private float _preauthChallengeTime;

	private float _delayVolumeResetTime;

	private float _rejectSuppressionTime;

	private float _issuedSuppressionTime;

	private bool _disconnectDrop;

	private static readonly int[] _loadingLogId = new int[4] { 13, 14, 17, 33 };

	private readonly HashSet<IPEndPoint> _dictToRemove = new HashSet<IPEndPoint>();

	private readonly HashSet<string> _dict2ToRemove = new HashSet<string>();

	private static ushort _ipRateLimitWindow;

	private static ushort _userIdLimitWindow;

	private static ushort _preauthChallengeWindow;

	private static ushort _preauthChallengeClean;

	public string disconnectMessage = "";

	public static string ConnectionIp;

	public static string LastIp;

	[Space(30f)]
	public int GameFilesVersion;

	public static bool Modded = false;

	private static readonly int _expectedGameFilesVersion = 4;

	public static int slots;

	public static int reservedSlots;

	public static bool EnableFastRestart = true;

	public static float FastRestartDelay = 3.2f;

	private const int IpRetryDelay = 180;

	public static CustomNetworkManager TypedSingleton => (CustomNetworkManager)LiteNetLib4MirrorNetworkManager.singleton;

	public int MaxPlayers
	{
		get
		{
			return base.maxConnections;
		}
		set
		{
			base.maxConnections = value;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = (ushort)value;
		}
	}

	public int ReservedMaxPlayers => CustomNetworkManager.slots;

	public static bool IsVerified { get; internal set; }

	public static event Action OnClientReady;

	public static event Action OnClientStarted;

	private new void Update()
	{
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		this._dictCleanupTime += Time.fixedUnscaledDeltaTime;
		this._ipRateLimitTime += Time.fixedUnscaledDeltaTime;
		this._userIdRateLimitTime += Time.fixedUnscaledDeltaTime;
		this._preauthChallengeTime += Time.fixedUnscaledDeltaTime;
		this._delayVolumeResetTime += Time.fixedUnscaledDeltaTime;
		this._rejectSuppressionTime += Time.fixedUnscaledDeltaTime;
		this._issuedSuppressionTime += Time.fixedUnscaledDeltaTime;
		if (this._ipRateLimitTime >= (float)(int)CustomNetworkManager._ipRateLimitWindow)
		{
			this._ipRateLimitTime = 0f;
			CustomLiteNetLib4MirrorTransport.IpRateLimit.Clear();
		}
		if (this._userIdRateLimitTime >= (float)(int)CustomNetworkManager._userIdLimitWindow)
		{
			this._userIdRateLimitTime = 0f;
			CustomLiteNetLib4MirrorTransport.UserRateLimit.Clear();
		}
		if (this._delayVolumeResetTime > 5.5f)
		{
			this._delayVolumeResetTime = 0f;
			CustomLiteNetLib4MirrorTransport.DelayVolume = 0;
		}
		if (this._rejectSuppressionTime > 10f)
		{
			this._rejectSuppressionTime = 0f;
			if (CustomLiteNetLib4MirrorTransport.SuppressRejections)
			{
				if (CustomLiteNetLib4MirrorTransport.Rejected <= CustomLiteNetLib4MirrorTransport.RejectionThreshold)
				{
					CustomLiteNetLib4MirrorTransport.SuppressRejections = false;
				}
				ServerConsole.AddLog($"{CustomLiteNetLib4MirrorTransport.Rejected} incoming connections have been rejected within the last 10 seconds.", ConsoleColor.Yellow);
			}
			CustomLiteNetLib4MirrorTransport.Rejected = 0u;
		}
		if (this._issuedSuppressionTime > 10f)
		{
			this._issuedSuppressionTime = 0f;
			if (CustomLiteNetLib4MirrorTransport.SuppressIssued)
			{
				if (CustomLiteNetLib4MirrorTransport.ChallengeIssued <= CustomLiteNetLib4MirrorTransport.IssuedThreshold)
				{
					CustomLiteNetLib4MirrorTransport.SuppressIssued = false;
				}
				ServerConsole.AddLog($"{CustomLiteNetLib4MirrorTransport.ChallengeIssued} challenges have been requested within the last 10 seconds.", ConsoleColor.Yellow);
			}
			CustomLiteNetLib4MirrorTransport.ChallengeIssued = 0u;
		}
		if (this._preauthChallengeTime >= (float)(int)CustomNetworkManager._preauthChallengeClean)
		{
			this._preauthChallengeTime = 0f;
			long ticks = DateTime.Now.AddSeconds(CustomNetworkManager._preauthChallengeWindow * -1).Ticks;
			foreach (KeyValuePair<string, PreauthChallengeItem> challenge in CustomLiteNetLib4MirrorTransport.Challenges)
			{
				if (challenge.Value.Added <= ticks)
				{
					this._dict2ToRemove.Add(challenge.Key);
				}
			}
			foreach (string item in this._dict2ToRemove)
			{
				if (CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(item))
				{
					CustomLiteNetLib4MirrorTransport.Challenges.Remove(item);
				}
			}
			this._dict2ToRemove.Clear();
		}
		if (this._dictCleanupTime <= 20f)
		{
			return;
		}
		this._dictCleanupTime = 0f;
		long ticks2 = DateTime.Now.AddSeconds(-200.0).Ticks;
		foreach (KeyValuePair<IPEndPoint, PreauthItem> userId in CustomLiteNetLib4MirrorTransport.UserIds)
		{
			if (userId.Value.Added <= ticks2)
			{
				this._dictToRemove.Add(userId.Key);
			}
		}
		foreach (IPEndPoint item2 in this._dictToRemove)
		{
			if (CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(item2))
			{
				CustomLiteNetLib4MirrorTransport.UserIds.Remove(item2);
			}
		}
		this._dictToRemove.Clear();
	}

	internal static void InvokeOnClientReady()
	{
		CustomNetworkManager.OnClientReady?.Invoke();
	}

	public override void OnClientConnect()
	{
		CustomNetworkManager.OnClientReady?.Invoke();
		base.OnClientConnect();
	}

	public override void ServerChangeScene(string newSceneName)
	{
		if (string.IsNullOrEmpty(newSceneName))
		{
			Debug.LogError("ServerChangeScene empty scene name");
			return;
		}
		if (NetworkServer.isLoadingScene && newSceneName == NetworkManager.networkSceneName)
		{
			Debug.LogError("Scene change is already in progress for " + newSceneName);
			return;
		}
		NetworkServer.SetAllClientsNotReady();
		NetworkManager.networkSceneName = newSceneName;
		this.OnServerChangeScene(newSceneName);
		NetworkServer.isLoadingScene = true;
		NetworkManager.loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
		if (NetworkServer.active)
		{
			if (CustomNetworkManager.EnableFastRestart)
			{
				Timing.CallDelayed(CustomNetworkManager.FastRestartDelay, delegate
				{
					NetworkServer.SendToAll(new SceneMessage
					{
						sceneName = newSceneName
					});
				});
			}
			else
			{
				NetworkServer.SendToAll(new SceneMessage
				{
					sceneName = newSceneName
				});
			}
		}
		NetworkManager.startPositionIndex = 0;
		NetworkManager.startPositions.Clear();
	}

	public override void OnClientDisconnect()
	{
		base.OnClientDisconnect();
	}

	private static void PrintConnectionDebug(string reason)
	{
		GameCore.Console.AddLog(reason, Color.red);
		GameCore.Console.AddLog("IP: " + LiteNetLib4MirrorTransport.Singleton.clientAddress, Color.red);
		GameCore.Console.AddLog("Port: " + LiteNetLib4MirrorTransport.Singleton.port, Color.red);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		CustomNetworkManager.OnClientStarted?.Invoke();
		base.StartCoroutine(this._ConnectToServer());
	}

	public override void StartClient()
	{
		bool allow = true;
		CustomNetworkManager.TryStartClientChecks.ForEach(delegate(Func<CustomNetworkManager, bool> x)
		{
			allow = x(this) && allow;
		});
		if (allow)
		{
			this.ShowLoadingScreen(0);
			base.StartClient();
		}
	}

	private IEnumerator<float> _ConnectToServer()
	{
		while (LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.ClientConnecting || LiteNetLib4MirrorCore.State == LiteNetLib4MirrorCore.States.ClientConnected)
		{
			if (NetworkClient.isConnected)
			{
				this.ShowLoadingScreen(2);
				break;
			}
			yield return 0f;
		}
	}

	public bool IsFacilityLoading()
	{
		if (this._curLogId != 17)
		{
			return this._curLogId == 18;
		}
		return true;
	}

	public override void OnServerDisconnect(NetworkConnectionToClient conn)
	{
		if (this._disconnectDrop)
		{
			NetworkIdentity identity = conn.identity;
			if (identity != null && ReferenceHub.TryGetHubNetID(identity.netId, out var hub) && hub.IsAlive())
			{
				hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Unknown));
			}
		}
		if (CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled)
		{
			int id = LiteNetLib4MirrorServer.Peers[conn.connectionId].Id;
			if (CustomLiteNetLib4MirrorTransport.RealIpAddresses.ContainsKey(id))
			{
				CustomLiteNetLib4MirrorTransport.RealIpAddresses.Remove(id);
			}
		}
		base.OnServerDisconnect(conn);
		conn.Disconnect();
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (scene.name.Contains("menu", StringComparison.OrdinalIgnoreCase))
		{
			this._curLogId = 0;
			if (!this._activated)
			{
				this._activated = true;
			}
		}
		if (!(CustomNetworkManager.reconnectTime <= 0f))
		{
			base.Invoke("Reconnect", 3f);
		}
	}

	public override void OnClientSceneChanged()
	{
		CustomNetworkManager.OnClientReady?.Invoke();
		base.OnClientSceneChanged();
		if (CustomNetworkManager.reconnectTime <= 0f && this.logs[this._curLogId].autoHideOnSceneLoad)
		{
			this.popup.SetActive(value: false);
			this.loadingpop.SetActive(value: false);
		}
	}

	public bool ShouldPlayIntensive()
	{
		if (this._curLogId != 13)
		{
			return this.IsFacilityLoading();
		}
		return true;
	}

	private void Reconnect()
	{
		if (!(CustomNetworkManager.reconnectTime <= 0f))
		{
			CustomNetworkManager.reconnecting = true;
			CustomLiteNetLib4MirrorTransport.DelayConnections = true;
			IdleMode.PauseIdleMode = true;
			base.Invoke("TryConnecting", CustomNetworkManager.reconnectTime);
			CustomNetworkManager.reconnectTime = 0f;
		}
	}

	public void TryConnecting()
	{
		if (CustomNetworkManager.reconnecting)
		{
			this.StartClient();
		}
	}

	public void StopReconnecting()
	{
		CustomNetworkManager.reconnecting = false;
		CustomNetworkManager.triggerReconnectTime = 0f;
		CustomNetworkManager.reconnectTime = 0f;
	}

	public void ShowLog(int id, string obj1 = "", string obj2 = "", string obj3 = "", string textOverride = null)
	{
	}

	public void ShowLoadingScreen(int id)
	{
	}

	private void LoadConfigs(bool firstTime = false)
	{
		if (!this._configLoaded)
		{
			this._configLoaded = true;
			if (File.Exists("hoster_policy.txt"))
			{
				ConfigFile.HosterPolicy = new YamlConfig("hoster_policy.txt");
			}
			else if (File.Exists(FileManager.GetAppFolder() + "hoster_policy.txt"))
			{
				ConfigFile.HosterPolicy = new YamlConfig(FileManager.GetAppFolder() + "hoster_policy.txt");
			}
			else
			{
				ConfigFile.HosterPolicy = new YamlConfig();
			}
			FileManager.RefreshAppFolder();
			if (!ServerStatic.IsDedicated)
			{
				ServerConsole.AddLog("Loading configs...");
				ConfigFile.ReloadGameConfigs(firstTime);
				ServerConsole.AddLog("Config file loaded!");
			}
		}
	}

	public override void Start()
	{
		base.Start();
		this.LoadConfigs(firstTime: true);
		if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && !File.Exists("/etc/ssl/certs/ca-certificates.crt"))
		{
			if (File.Exists("/etc/pki/tls/certs/ca-bundle.crt"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/tls/certs/ca-bundle.crt on your system, please symlink your store to the required location!");
			}
			else if (File.Exists("/etc/ssl/ca-bundle.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/ssl/ca-bundle.pem on your system, please symlink your store to the required location!");
			}
			else if (File.Exists("/etc/pki/tls/cacert.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/tls/cacert.pem on your system, please symlink your store to the required location!");
			}
			else if (File.Exists("/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem on your system, please symlink your store to the required location!");
			}
			else
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt and we couldn't detect its location! Please provide access to it in the specified path!");
			}
		}
		if (ServerStatic.IsDedicated)
		{
			ServerConsole.RunRefreshPublicKey();
		}
	}

	public void CreateMatch()
	{
		ServerConsole.AddLog("Game version: " + GameCore.Version.VersionString);
		if (GameCore.Version.PrivateBeta)
		{
			ServerConsole.AddLog("PRIVATE BETA VERSION - DO NOT SHARE");
		}
		if (this.GameFilesVersion != CustomNetworkManager._expectedGameFilesVersion)
		{
			ServerConsole.AddLog("This source code file is made for different version of the game!");
			ServerConsole.AddLog("Please validate game files integrity using steam!");
			ServerConsole.AddLog("Aborting server startup.");
			return;
		}
		CustomLiteNetLib4MirrorTransport.DelayConnections = true;
		IdleMode.PauseIdleMode = true;
		this.LoadConfigs();
		this.ShowLoadingScreen(0);
		this.createpop.SetActive(value: false);
		ServerConsole.AddLog("Loading configs...");
		ConfigFile.ReloadGameConfigs();
		LiteNetLib4MirrorTransport.Singleton.port = (ServerStatic.IsDedicated ? ServerStatic.ServerPort : this.GetFreePort());
		SteamServerInfo.ServerPort = LiteNetLib4MirrorTransport.Singleton.port;
		LiteNetLib4MirrorTransport.Singleton.useUpnP = ConfigFile.ServerConfig.GetBool("forward_ports", def: true);
		CustomNetworkManager.slots = ConfigFile.ServerConfig.GetInt("max_players", 20);
		SteamServerInfo.MaxPlayers = CustomNetworkManager.slots;
		CustomLiteNetLib4MirrorTransport.DelayVolumeThreshold = (byte)(Mathf.Clamp(CustomNetworkManager.slots, 5, 125) * 2);
		CustomNetworkManager.reservedSlots = Mathf.Max(ConfigFile.ServerConfig.GetInt("reserved_slots", ReservedSlot.Users.Count), 0);
		this._disconnectDrop = ConfigFile.ServerConfig.GetBool("disconnect_drop", def: true);
		this.MaxPlayers = (CustomNetworkManager.slots + CustomNetworkManager.reservedSlots) * 2 + 50;
		int num = ConfigFile.HosterPolicy.GetInt("players_limit", -1);
		if (num > 0 && CustomNetworkManager.slots + CustomNetworkManager.reservedSlots > num)
		{
			this.MaxPlayers = num * 2 + 50;
			ServerConsole.AddLog("You have exceeded players limit set by your hosting provider. Max players value set to " + num);
		}
		ServerConsole.AddLog("Config files loaded from " + FileManager.GetAppFolder(addSeparator: true, serverConfig: true));
		this._queryEnabled = ConfigFile.ServerConfig.GetBool("enable_query");
		string text = FileManager.GetAppFolder(addSeparator: true, serverConfig: true) + "config_remoteadmin.txt";
		if (!File.Exists(text))
		{
			File.Copy("ConfigTemplates/config_remoteadmin.template.txt", text);
		}
		ServerConsole.AddLog("Loading server permissions configuration...");
		ServerStatic.RolesConfigPath = text;
		ServerStatic.RolesConfig = new YamlConfig(text);
		ServerStatic.SharedGroupsConfig = ((ConfigSharing.Paths[4] == null) ? null : new YamlConfig(ConfigSharing.Paths[4] + "shared_groups.txt"));
		ServerStatic.SharedGroupsMembersConfig = ((ConfigSharing.Paths[5] == null) ? null : new YamlConfig(ConfigSharing.Paths[5] + "shared_groups_members.txt"));
		ServerStatic.PermissionsHandler = new PermissionsHandler(ref ServerStatic.RolesConfig, ref ServerStatic.SharedGroupsConfig, ref ServerStatic.SharedGroupsMembersConfig);
		ServerConsole.AddLog("Server permissions configuration loaded.");
		CustomLiteNetLib4MirrorTransport.UseGlobalBans = ConfigFile.ServerConfig.GetBool("use_global_bans", def: true);
		CustomLiteNetLib4MirrorTransport.ReloadChallengeOptions();
		ServerConsole.AddLog(PlayerAuthenticationManager.OnlineMode ? "Online mode is ENABLED." : "Online mode is DISABLED - SERVER CANNOT VALIDATE USER ID OF CONNECTING PLAYERS!!! Features like User ID admin authentication won't work.");
		ServerConsole.AddLog("Starting server...");
		Timing.RunCoroutine(this._CreateLobby());
	}

	internal static void ReloadTimeWindows()
	{
		CustomNetworkManager._ipRateLimitWindow = ConfigFile.ServerConfig.GetUShort("ip_ratelimit_window", 3);
		CustomNetworkManager._userIdLimitWindow = ConfigFile.ServerConfig.GetUShort("userid_ratelimit_window", 5);
		CustomNetworkManager._preauthChallengeWindow = ConfigFile.ServerConfig.GetUShort("preauth_challenge_time_window", 8);
		CustomNetworkManager._preauthChallengeClean = ConfigFile.ServerConfig.GetUShort("preauth_challenge_clean_period", 4);
		if (CustomNetworkManager._ipRateLimitWindow == 0)
		{
			CustomNetworkManager._ipRateLimitWindow = 1;
		}
		if (CustomNetworkManager._userIdLimitWindow == 0)
		{
			CustomNetworkManager._userIdLimitWindow = 1;
		}
		if (CustomNetworkManager._preauthChallengeWindow == 0)
		{
			CustomNetworkManager._preauthChallengeWindow = 1;
		}
		if (CustomNetworkManager._preauthChallengeClean == 0)
		{
			CustomNetworkManager._preauthChallengeClean = 1;
		}
	}

	private IEnumerator<float> _CreateLobby()
	{
		if (this._queryEnabled)
		{
			CustomNetworkManager.QueryServer = new QueryServer(LiteNetLib4MirrorTransport.Singleton.port + ConfigFile.ServerConfig.GetInt("query_port_shift"), ConfigFile.ServerConfig.GetString("query_bind_ip1", "0.0.0.0"), ConfigFile.ServerConfig.GetString("query_bind_ip2", "::"), ConfigFile.ServerConfig.GetString("query_administrator_password"));
			CustomNetworkManager.QueryServer.StartServer();
		}
		else
		{
			ServerConsole.AddLog("Query port disabled in config!");
		}
		if (ConfigFile.HosterPolicy.GetString("server_ip", "none") != "none")
		{
			ServerConsole.Ip = ConfigFile.HosterPolicy.GetString("server_ip", "none");
			ServerConsole.AddLog("Server IP address set to " + ServerConsole.Ip + " by your hosting provider.");
		}
		else if (PlayerAuthenticationManager.OnlineMode)
		{
			if (ConfigFile.ServerConfig.GetString("server_ip", "auto") != "auto")
			{
				ServerConsole.Ip = ConfigFile.ServerConfig.GetString("server_ip", "auto");
				ServerConsole.AddLog("Custom config detected. Your server IP address is " + ServerConsole.Ip);
			}
			else
			{
				ServerConsole.AddLog("Obtaining your external IP address...");
				while (true)
				{
					using (UnityWebRequest www = UnityWebRequest.Get(CentralServer.StandardUrl + "ip.php"))
					{
						yield return Timing.WaitUntilDone(www.SendWebRequest());
						if (string.IsNullOrEmpty(www.error))
						{
							ServerConsole.Ip = www.downloadHandler.text;
							ServerConsole.AddLog("Done, your server IP address is " + ServerConsole.Ip);
							break;
						}
						ServerConsole.AddLog("Error: connection to " + CentralServer.StandardUrl + " failed. Website returned: " + www.error + " | Retrying in " + 180 + " seconds...", ConsoleColor.DarkRed);
					}
					yield return Timing.WaitForSeconds(180f);
				}
			}
		}
		else
		{
			ServerConsole.Ip = "127.0.0.1";
		}
		ServerConsole.AddLog("Initializing game server...");
		if (ConfigFile.HosterPolicy.GetString("ipv4_bind_ip", "none") != "none")
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = ConfigFile.HosterPolicy.GetString("ipv4_bind_ip", "0.0.0.0");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress == "0.0.0.0")
			{
				ServerConsole.AddLog("Server starting at all IPv4 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port + " - set by your hosting provider.");
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv4 " + LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port + " - set by your hosting provider.");
			}
		}
		else
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = ConfigFile.ServerConfig.GetString("ipv4_bind_ip", "0.0.0.0");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress == "0.0.0.0")
			{
				ServerConsole.AddLog("Server starting at all IPv4 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port);
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv4 " + LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port);
			}
		}
		if (ConfigFile.HosterPolicy.GetString("ipv6_bind_ip", "none") != "none")
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = ConfigFile.HosterPolicy.GetString("ipv6_bind_ip", "::");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress == "::")
			{
				ServerConsole.AddLog("Server starting at all IPv6 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port + " - set by your hosting provider.");
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv6 " + LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port + " - set by your hosting provider.");
			}
		}
		else
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = ConfigFile.ServerConfig.GetString("ipv6_bind_ip", "::");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress == "::")
			{
				ServerConsole.AddLog("Server starting at all IPv6 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port);
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv6 " + LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port);
			}
		}
		if (ServerConsole.PublicKey == null && PlayerAuthenticationManager.OnlineMode)
		{
			ServerConsole.AddLog("Central server public key is not loaded. Waiting...");
			while (ServerConsole.PublicKey == null)
			{
				yield return Timing.WaitForSeconds(0.25f);
			}
			ServerConsole.AddLog("Continuing server startup sequence...");
		}
		LiteNetLib4MirrorTransport.Singleton.useNativeSockets = ConfigFile.ServerConfig.GetBool("use_native_sockets", def: true);
		ServerConsole.AddLog("Network sockets mode: " + (LiteNetLib4MirrorTransport.Singleton.useNativeSockets ? "Native" : "Unity"));
		base.StartHost();
		while (SceneManager.GetActiveScene().name != "Facility")
		{
			yield return float.NegativeInfinity;
		}
		ServerConsole.AddLog("Level loaded. Creating match...");
		if (!PlayerAuthenticationManager.OnlineMode)
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to online_mode turned off in server configuration.", ConsoleColor.DarkRed);
		}
		else if (ConfigFile.ServerConfig.GetBool("disable_global_badges"))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to disable_global_badges turned on in server configuration (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed);
		}
		else if (ConfigFile.ServerConfig.GetBool("hide_global_badges"))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to hide_global_badges turned on in server configuration. You can still disable specific badges instead of using this command. (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed);
		}
		else if (ConfigFile.ServerConfig.GetBool("disable_ban_bypass"))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to disable_ban_bypass turned on in server configuration. (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed);
		}
		else
		{
			ServerConsole.Singleton.RunServer();
		}
	}

	public ushort GetFreePort()
	{
		return ServerStatic.ServerPort;
	}
}
