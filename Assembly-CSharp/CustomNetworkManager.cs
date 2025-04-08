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
	public static CustomNetworkManager TypedSingleton
	{
		get
		{
			return (CustomNetworkManager)LiteNetLib4MirrorNetworkManager.singleton;
		}
	}

	public static event Action OnClientReady;

	public static event Action OnClientStarted;

	public int MaxPlayers
	{
		get
		{
			return this.maxConnections;
		}
		set
		{
			this.maxConnections = value;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = (ushort)value;
		}
	}

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
		if (this._ipRateLimitTime >= (float)CustomNetworkManager._ipRateLimitWindow)
		{
			this._ipRateLimitTime = 0f;
			CustomLiteNetLib4MirrorTransport.IpRateLimit.Clear();
		}
		if (this._userIdRateLimitTime >= (float)CustomNetworkManager._userIdLimitWindow)
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
				ServerConsole.AddLog(string.Format("{0} incoming connections have been rejected within the last 10 seconds.", CustomLiteNetLib4MirrorTransport.Rejected), ConsoleColor.Yellow, false);
			}
			CustomLiteNetLib4MirrorTransport.Rejected = 0U;
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
				ServerConsole.AddLog(string.Format("{0} challenges have been requested within the last 10 seconds.", CustomLiteNetLib4MirrorTransport.ChallengeIssued), ConsoleColor.Yellow, false);
			}
			CustomLiteNetLib4MirrorTransport.ChallengeIssued = 0U;
		}
		if (this._preauthChallengeTime >= (float)CustomNetworkManager._preauthChallengeClean)
		{
			this._preauthChallengeTime = 0f;
			long ticks = DateTime.Now.AddSeconds((double)((int)CustomNetworkManager._preauthChallengeWindow * -1)).Ticks;
			foreach (KeyValuePair<string, PreauthChallengeItem> keyValuePair in CustomLiteNetLib4MirrorTransport.Challenges)
			{
				if (keyValuePair.Value.Added <= ticks)
				{
					this._dict2ToRemove.Add(keyValuePair.Key);
				}
			}
			foreach (string text in this._dict2ToRemove)
			{
				if (CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(text))
				{
					CustomLiteNetLib4MirrorTransport.Challenges.Remove(text);
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
		foreach (KeyValuePair<IPEndPoint, PreauthItem> keyValuePair2 in CustomLiteNetLib4MirrorTransport.UserIds)
		{
			if (keyValuePair2.Value.Added <= ticks2)
			{
				this._dictToRemove.Add(keyValuePair2.Key);
			}
		}
		foreach (IPEndPoint ipendPoint in this._dictToRemove)
		{
			if (CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(ipendPoint))
			{
				CustomLiteNetLib4MirrorTransport.UserIds.Remove(ipendPoint);
			}
		}
		this._dictToRemove.Clear();
	}

	internal static void InvokeOnClientReady()
	{
		Action onClientReady = CustomNetworkManager.OnClientReady;
		if (onClientReady == null)
		{
			return;
		}
		onClientReady();
	}

	public override void OnClientConnect()
	{
		Action onClientReady = CustomNetworkManager.OnClientReady;
		if (onClientReady != null)
		{
			onClientReady();
		}
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
					NetworkServer.SendToAll<SceneMessage>(new SceneMessage
					{
						sceneName = newSceneName
					}, 0, false);
				});
			}
			else
			{
				NetworkServer.SendToAll<SceneMessage>(new SceneMessage
				{
					sceneName = newSceneName
				}, 0, false);
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
		global::GameCore.Console.AddLog(reason, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
		global::GameCore.Console.AddLog("IP: " + LiteNetLib4MirrorTransport.Singleton.clientAddress, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
		global::GameCore.Console.AddLog("Port: " + LiteNetLib4MirrorTransport.Singleton.port.ToString(), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		Action onClientStarted = CustomNetworkManager.OnClientStarted;
		if (onClientStarted != null)
		{
			onClientStarted();
		}
		base.StartCoroutine(this._ConnectToServer());
	}

	public override void StartClient()
	{
		bool allow = true;
		CustomNetworkManager.TryStartClientChecks.ForEach(delegate(Func<CustomNetworkManager, bool> x)
		{
			allow = x(this) & allow;
		});
		if (!allow)
		{
			return;
		}
		this.ShowLoadingScreen(0);
		base.StartClient();
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
		yield break;
	}

	public bool IsFacilityLoading()
	{
		return this._curLogId == 17 || this._curLogId == 18;
	}

	public override void OnServerDisconnect(NetworkConnectionToClient conn)
	{
		if (this._disconnectDrop)
		{
			NetworkIdentity identity = conn.identity;
			ReferenceHub referenceHub;
			if (identity != null && ReferenceHub.TryGetHubNetID(identity.netId, out referenceHub) && referenceHub.IsAlive())
			{
				referenceHub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Unknown, null));
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
		if (CustomNetworkManager.reconnectTime <= 0f)
		{
			return;
		}
		base.Invoke("Reconnect", 3f);
	}

	public override void OnClientSceneChanged()
	{
		Action onClientReady = CustomNetworkManager.OnClientReady;
		if (onClientReady != null)
		{
			onClientReady();
		}
		base.OnClientSceneChanged();
		if (CustomNetworkManager.reconnectTime <= 0f && this.logs[this._curLogId].autoHideOnSceneLoad)
		{
			this.popup.SetActive(false);
			this.loadingpop.SetActive(false);
		}
	}

	public bool ShouldPlayIntensive()
	{
		return this._curLogId == 13 || this.IsFacilityLoading();
	}

	private void Reconnect()
	{
		if (CustomNetworkManager.reconnectTime <= 0f)
		{
			return;
		}
		CustomNetworkManager.reconnecting = true;
		CustomLiteNetLib4MirrorTransport.DelayConnections = true;
		IdleMode.PauseIdleMode = true;
		base.Invoke("TryConnecting", CustomNetworkManager.reconnectTime);
		CustomNetworkManager.reconnectTime = 0f;
	}

	public void TryConnecting()
	{
		if (!CustomNetworkManager.reconnecting)
		{
			return;
		}
		this.StartClient();
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

	public int ReservedMaxPlayers
	{
		get
		{
			return CustomNetworkManager.slots;
		}
	}

	public static bool IsVerified { get; internal set; }

	private void LoadConfigs(bool firstTime = false)
	{
		if (this._configLoaded)
		{
			return;
		}
		this._configLoaded = true;
		if (File.Exists("hoster_policy.txt"))
		{
			ConfigFile.HosterPolicy = new YamlConfig("hoster_policy.txt");
		}
		else if (File.Exists(FileManager.GetAppFolder(true, false, "") + "hoster_policy.txt"))
		{
			ConfigFile.HosterPolicy = new YamlConfig(FileManager.GetAppFolder(true, false, "") + "hoster_policy.txt");
		}
		else
		{
			ConfigFile.HosterPolicy = new YamlConfig();
		}
		FileManager.RefreshAppFolder();
		if (ServerStatic.IsDedicated)
		{
			return;
		}
		ServerConsole.AddLog("Loading configs...", ConsoleColor.Gray, false);
		ConfigFile.ReloadGameConfigs(firstTime);
		ServerConsole.AddLog("Config file loaded!", ConsoleColor.Gray, false);
	}

	public override void Start()
	{
		base.Start();
		this.LoadConfigs(true);
		if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux && !File.Exists("/etc/ssl/certs/ca-certificates.crt"))
		{
			if (File.Exists("/etc/pki/tls/certs/ca-bundle.crt"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/tls/certs/ca-bundle.crt on your system, please symlink your store to the required location!", ConsoleColor.Gray, false);
			}
			else if (File.Exists("/etc/ssl/ca-bundle.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/ssl/ca-bundle.pem on your system, please symlink your store to the required location!", ConsoleColor.Gray, false);
			}
			else if (File.Exists("/etc/pki/tls/cacert.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/tls/cacert.pem on your system, please symlink your store to the required location!", ConsoleColor.Gray, false);
			}
			else if (File.Exists("/etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem"))
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt, but we've detected it's present in /etc/pki/ca-trust/extracted/pem/tls-ca-bundle.pem on your system, please symlink your store to the required location!", ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog("System CA Cert store not available! Unity expects it to be in /etc/ssl/certs/ca-certificates.crt and we couldn't detect its location! Please provide access to it in the specified path!", ConsoleColor.Gray, false);
			}
		}
		if (ServerStatic.IsDedicated)
		{
			ServerConsole.RunRefreshPublicKey();
		}
	}

	public void CreateMatch()
	{
		ServerConsole.AddLog("Game version: " + global::GameCore.Version.VersionString, ConsoleColor.Gray, false);
		if (global::GameCore.Version.PrivateBeta)
		{
			ServerConsole.AddLog("PRIVATE BETA VERSION - DO NOT SHARE", ConsoleColor.Gray, false);
		}
		if (this.GameFilesVersion != CustomNetworkManager._expectedGameFilesVersion)
		{
			ServerConsole.AddLog("This source code file is made for different version of the game!", ConsoleColor.Gray, false);
			ServerConsole.AddLog("Please validate game files integrity using steam!", ConsoleColor.Gray, false);
			ServerConsole.AddLog("Aborting server startup.", ConsoleColor.Gray, false);
			return;
		}
		CustomLiteNetLib4MirrorTransport.DelayConnections = true;
		IdleMode.PauseIdleMode = true;
		this.LoadConfigs(false);
		this.ShowLoadingScreen(0);
		this.createpop.SetActive(false);
		ServerConsole.AddLog("Loading configs...", ConsoleColor.Gray, false);
		ConfigFile.ReloadGameConfigs(false);
		LiteNetLib4MirrorTransport.Singleton.port = (ServerStatic.IsDedicated ? ServerStatic.ServerPort : this.GetFreePort());
		SteamServerInfo.ServerPort = LiteNetLib4MirrorTransport.Singleton.port;
		LiteNetLib4MirrorTransport.Singleton.useUpnP = ConfigFile.ServerConfig.GetBool("forward_ports", true);
		CustomNetworkManager.slots = ConfigFile.ServerConfig.GetInt("max_players", 20);
		SteamServerInfo.MaxPlayers = CustomNetworkManager.slots;
		CustomLiteNetLib4MirrorTransport.DelayVolumeThreshold = (byte)(Mathf.Clamp(CustomNetworkManager.slots, 5, 125) * 2);
		CustomNetworkManager.reservedSlots = Mathf.Max(ConfigFile.ServerConfig.GetInt("reserved_slots", ReservedSlot.Users.Count), 0);
		this._disconnectDrop = ConfigFile.ServerConfig.GetBool("disconnect_drop", true);
		this.MaxPlayers = (CustomNetworkManager.slots + CustomNetworkManager.reservedSlots) * 2 + 50;
		int @int = ConfigFile.HosterPolicy.GetInt("players_limit", -1);
		if (@int > 0 && CustomNetworkManager.slots + CustomNetworkManager.reservedSlots > @int)
		{
			this.MaxPlayers = @int * 2 + 50;
			ServerConsole.AddLog("You have exceeded players limit set by your hosting provider. Max players value set to " + @int.ToString(), ConsoleColor.Gray, false);
		}
		ServerConsole.AddLog("Config files loaded from " + FileManager.GetAppFolder(true, true, ""), ConsoleColor.Gray, false);
		this._queryEnabled = ConfigFile.ServerConfig.GetBool("enable_query", false);
		string text = FileManager.GetAppFolder(true, true, "") + "config_remoteadmin.txt";
		if (!File.Exists(text))
		{
			File.Copy("ConfigTemplates/config_remoteadmin.template.txt", text);
		}
		ServerConsole.AddLog("Loading server permissions configuration...", ConsoleColor.Gray, false);
		ServerStatic.RolesConfigPath = text;
		ServerStatic.RolesConfig = new YamlConfig(text);
		ServerStatic.SharedGroupsConfig = ((ConfigSharing.Paths[4] == null) ? null : new YamlConfig(ConfigSharing.Paths[4] + "shared_groups.txt"));
		ServerStatic.SharedGroupsMembersConfig = ((ConfigSharing.Paths[5] == null) ? null : new YamlConfig(ConfigSharing.Paths[5] + "shared_groups_members.txt"));
		ServerStatic.PermissionsHandler = new PermissionsHandler(ref ServerStatic.RolesConfig, ref ServerStatic.SharedGroupsConfig, ref ServerStatic.SharedGroupsMembersConfig);
		ServerConsole.AddLog("Server permissions configuration loaded.", ConsoleColor.Gray, false);
		CustomLiteNetLib4MirrorTransport.UseGlobalBans = ConfigFile.ServerConfig.GetBool("use_global_bans", true);
		CustomLiteNetLib4MirrorTransport.ReloadChallengeOptions();
		ServerConsole.AddLog(PlayerAuthenticationManager.OnlineMode ? "Online mode is ENABLED." : "Online mode is DISABLED - SERVER CANNOT VALIDATE USER ID OF CONNECTING PLAYERS!!! Features like User ID admin authentication won't work.", ConsoleColor.Gray, false);
		ServerConsole.AddLog("Starting server...", ConsoleColor.Gray, false);
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
			CustomNetworkManager.QueryServer = new QueryServer((int)LiteNetLib4MirrorTransport.Singleton.port + ConfigFile.ServerConfig.GetInt("query_port_shift", 0), ConfigFile.ServerConfig.GetString("query_bind_ip1", "0.0.0.0"), ConfigFile.ServerConfig.GetString("query_bind_ip2", "::"), ConfigFile.ServerConfig.GetString("query_administrator_password", ""));
			CustomNetworkManager.QueryServer.StartServer();
		}
		else
		{
			ServerConsole.AddLog("Query port disabled in config!", ConsoleColor.Gray, false);
		}
		if (ConfigFile.HosterPolicy.GetString("server_ip", "none") != "none")
		{
			ServerConsole.Ip = ConfigFile.HosterPolicy.GetString("server_ip", "none");
			ServerConsole.AddLog("Server IP address set to " + ServerConsole.Ip + " by your hosting provider.", ConsoleColor.Gray, false);
		}
		else if (PlayerAuthenticationManager.OnlineMode)
		{
			if (ConfigFile.ServerConfig.GetString("server_ip", "auto") != "auto")
			{
				ServerConsole.Ip = ConfigFile.ServerConfig.GetString("server_ip", "auto");
				ServerConsole.AddLog("Custom config detected. Your server IP address is " + ServerConsole.Ip, ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog("Obtaining your external IP address...", ConsoleColor.Gray, false);
				for (;;)
				{
					using (UnityWebRequest www = UnityWebRequest.Get(CentralServer.StandardUrl + "ip.php"))
					{
						yield return Timing.WaitUntilDone(www.SendWebRequest());
						if (string.IsNullOrEmpty(www.error))
						{
							ServerConsole.Ip = www.downloadHandler.text;
							ServerConsole.AddLog("Done, your server IP address is " + ServerConsole.Ip, ConsoleColor.Gray, false);
							break;
						}
						ServerConsole.AddLog(string.Concat(new string[]
						{
							"Error: connection to ",
							CentralServer.StandardUrl,
							" failed. Website returned: ",
							www.error,
							" | Retrying in ",
							180.ToString(),
							" seconds..."
						}), ConsoleColor.DarkRed, false);
					}
					UnityWebRequest www = null;
					yield return Timing.WaitForSeconds(180f);
				}
			}
		}
		else
		{
			ServerConsole.Ip = "127.0.0.1";
		}
		ServerConsole.AddLog("Initializing game server...", ConsoleColor.Gray, false);
		if (ConfigFile.HosterPolicy.GetString("ipv4_bind_ip", "none") != "none")
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = ConfigFile.HosterPolicy.GetString("ipv4_bind_ip", "0.0.0.0");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress == "0.0.0.0")
			{
				ServerConsole.AddLog("Server starting at all IPv4 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString() + " - set by your hosting provider.", ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog(string.Concat(new string[]
				{
					"Server starting at IPv4 ",
					LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress,
					" and port ",
					LiteNetLib4MirrorTransport.Singleton.port.ToString(),
					" - set by your hosting provider."
				}), ConsoleColor.Gray, false);
			}
		}
		else
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = ConfigFile.ServerConfig.GetString("ipv4_bind_ip", "0.0.0.0");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress == "0.0.0.0")
			{
				ServerConsole.AddLog("Server starting at all IPv4 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString(), ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv4 " + LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString(), ConsoleColor.Gray, false);
			}
		}
		if (ConfigFile.HosterPolicy.GetString("ipv6_bind_ip", "none") != "none")
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = ConfigFile.HosterPolicy.GetString("ipv6_bind_ip", "::");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress == "::")
			{
				ServerConsole.AddLog("Server starting at all IPv6 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString() + " - set by your hosting provider.", ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog(string.Concat(new string[]
				{
					"Server starting at IPv6 ",
					LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress,
					" and port ",
					LiteNetLib4MirrorTransport.Singleton.port.ToString(),
					" - set by your hosting provider."
				}), ConsoleColor.Gray, false);
			}
		}
		else
		{
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = ConfigFile.ServerConfig.GetString("ipv6_bind_ip", "::");
			if (LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress == "::")
			{
				ServerConsole.AddLog("Server starting at all IPv6 addresses and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString(), ConsoleColor.Gray, false);
			}
			else
			{
				ServerConsole.AddLog("Server starting at IPv6 " + LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress + " and port " + LiteNetLib4MirrorTransport.Singleton.port.ToString(), ConsoleColor.Gray, false);
			}
		}
		if (ServerConsole.PublicKey == null && PlayerAuthenticationManager.OnlineMode)
		{
			ServerConsole.AddLog("Central server public key is not loaded. Waiting...", ConsoleColor.Gray, false);
			while (ServerConsole.PublicKey == null)
			{
				yield return Timing.WaitForSeconds(0.25f);
			}
			ServerConsole.AddLog("Continuing server startup sequence...", ConsoleColor.Gray, false);
		}
		LiteNetLib4MirrorTransport.Singleton.useNativeSockets = ConfigFile.ServerConfig.GetBool("use_native_sockets", true);
		ServerConsole.AddLog("Network sockets mode: " + (LiteNetLib4MirrorTransport.Singleton.useNativeSockets ? "Native" : "Unity"), ConsoleColor.Gray, false);
		base.StartHost();
		while (SceneManager.GetActiveScene().name != "Facility")
		{
			yield return float.NegativeInfinity;
		}
		ServerConsole.AddLog("Level loaded. Creating match...", ConsoleColor.Gray, false);
		if (!PlayerAuthenticationManager.OnlineMode)
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to online_mode turned off in server configuration.", ConsoleColor.DarkRed, false);
			yield break;
		}
		if (ConfigFile.ServerConfig.GetBool("disable_global_badges", false))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to disable_global_badges turned on in server configuration (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed, false);
			yield break;
		}
		if (ConfigFile.ServerConfig.GetBool("hide_global_badges", false))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to hide_global_badges turned on in server configuration. You can still disable specific badges instead of using this command. (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed, false);
			yield break;
		}
		if (ConfigFile.ServerConfig.GetBool("disable_ban_bypass", false))
		{
			ServerConsole.AddLog("Server WON'T be visible on the public list due to disable_ban_bypass turned on in server configuration. (this is servermod function - if you are not using servermod, you can safely remove this config value, it won't change anything).", ConsoleColor.DarkRed, false);
			yield break;
		}
		ServerConsole.Singleton.RunServer();
		yield break;
		yield break;
	}

	public ushort GetFreePort()
	{
		return ServerStatic.ServerPort;
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

	public CustomNetworkManager.DisconnectLog[] logs;

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

	private static readonly int[] _loadingLogId = new int[] { 13, 14, 17, 33 };

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

	[Serializable]
	public class DisconnectLog
	{
		[Multiline]
		public string msg_en;

		public CustomNetworkManager.DisconnectLog.LogButton button;

		public bool autoHideOnSceneLoad;

		[Serializable]
		public class LogButton
		{
			public ConnInfoButton[] actions;
		}
	}
}
