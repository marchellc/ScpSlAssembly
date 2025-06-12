using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Cryptography;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using Mirror.RemoteCalls;
using NetworkManagerUtils.Dummies;
using NorthwoodLib;
using UnityEngine;
using VoiceChat;

namespace CentralAuth;

public class PlayerAuthenticationManager : NetworkBehaviour
{
	[SyncVar(hook = "UserIdHook")]
	public string SyncedUserId;

	public static bool OnlineMode;

	internal static bool AllowSameAccountJoining;

	public static uint AuthenticationTimeout;

	private static readonly Regex _saltRegex;

	private bool _authenticationRequested;

	private float _timeoutTimer;

	private float _passwordCooldown;

	private string _privUserId;

	private string _challenge;

	private string _clientSalt;

	private uint _passwordAttempts;

	private ReferenceHub _hub;

	private ClientInstanceMode _targetInstanceMode;

	private const int HashIterations = 1600;

	private const string HostId = "ID_Host";

	private const string DedicatedId = "ID_Dedicated";

	private const string OfflineModeIdPrefix = "ID_Offline_";

	private const string DummyId = "ID_Dummy";

	public AuthenticationResponse AuthenticationResponse { get; private set; }

	public bool DoNotTrack { get; private set; }

	public string UserId
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this.SyncedUserId;
			}
			if (this._privUserId == null)
			{
				return null;
			}
			if (this._privUserId.Contains("$"))
			{
				return this._privUserId.Substring(0, this._privUserId.IndexOf("$", StringComparison.Ordinal));
			}
			return this._privUserId;
		}
		set
		{
			if (NetworkServer.active)
			{
				this._privUserId = value;
				this.UserIdHook(null, value);
				this.RefreshSyncedId();
				this._hub.serverRoles.RefreshRealId();
			}
		}
	}

	public string SaltedUserId
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this.SyncedUserId;
			}
			return this._privUserId;
		}
	}

	public ClientInstanceMode InstanceMode
	{
		get
		{
			return this._targetInstanceMode;
		}
		private set
		{
			if (value != this._targetInstanceMode)
			{
				this._targetInstanceMode = value;
				PlayerAuthenticationManager.OnInstanceModeChanged?.Invoke(this._hub, this._targetInstanceMode);
			}
		}
	}

	public bool NorthwoodStaff => this.AuthenticationResponse.BadgeToken?.Staff ?? false;

	public bool BypassBansFlagSet => this.AuthenticationResponse.AuthToken?.BypassBans ?? false;

	public bool RemoteAdminGlobalAccess
	{
		get
		{
			BadgeToken badgeToken = this.AuthenticationResponse.BadgeToken;
			if (badgeToken != null && (badgeToken.Management || badgeToken.GlobalBanning))
			{
				return true;
			}
			return false;
		}
	}

	public string NetworkSyncedUserId
	{
		get
		{
			return this.SyncedUserId;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SyncedUserId, 1uL, UserIdHook);
		}
	}

	public static event Action<ReferenceHub> OnSyncedUserIdAssigned;

	public static event Action<ReferenceHub, ClientInstanceMode> OnInstanceModeChanged;

	[Server]
	private void RefreshSyncedId()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::RefreshSyncedId()' called when server was not active");
			return;
		}
		if (this._privUserId == null)
		{
			this.NetworkSyncedUserId = null;
			return;
		}
		bool flag = base.isLocalPlayer || this._hub.IsDummy || (this._privUserId.EndsWith("@steam", StringComparison.Ordinal) && !this.DoNotTrack && !this.AuthenticationResponse.AuthToken.SyncHashed);
		this.NetworkSyncedUserId = (flag ? this._privUserId : Sha.HashToString(Sha.Sha512(this._privUserId)));
	}

	private void Awake()
	{
		this._hub = ReferenceHub.GetHub(this);
	}

	private void FixedUpdate()
	{
		if (this._passwordCooldown > 0f)
		{
			this._passwordCooldown -= Time.fixedDeltaTime;
		}
		if (this.InstanceMode == ClientInstanceMode.Unverified && NetworkServer.active && PlayerAuthenticationManager.OnlineMode && !base.isLocalPlayer && !(this._timeoutTimer < 0f))
		{
			if (!this._authenticationRequested && base.connectionToClient.isReady)
			{
				this.RequestAuthentication();
			}
			this._timeoutTimer += Time.fixedDeltaTime;
			if (!(this._timeoutTimer <= (float)PlayerAuthenticationManager.AuthenticationTimeout))
			{
				this._timeoutTimer = -1f;
				this.RejectAuthentication("authentication timeout exceeded.");
			}
		}
	}

	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (base.isLocalPlayer)
		{
			NetworkServer.ReplaceHandler<AuthenticationResponse>(ServerReceiveAuthenticationResponse);
		}
		if (base.connectionToClient is DummyNetworkConnection)
		{
			this.UserId = "ID_Dummy";
		}
		else if (base.isLocalPlayer && ServerStatic.IsDedicated)
		{
			this.UserId = "ID_Dedicated";
		}
		else if (base.isLocalPlayer)
		{
			this.UserId = "ID_Host";
			if (PlayerAuthenticationManager.OnlineMode)
			{
				this.RequestAuthentication();
			}
		}
		else if (!PlayerAuthenticationManager.OnlineMode)
		{
			this.UserId = "ID_Offline_" + base.netId + "_" + DateTimeOffset.Now.ToUnixTimeSeconds();
		}
	}

	private static void ServerReceiveAuthenticationResponse(NetworkConnection conn, AuthenticationResponse msg)
	{
		if (NetworkServer.active && PlayerAuthenticationManager.OnlineMode && ReferenceHub.TryGetHub(conn, out var hub))
		{
			hub.authManager.ProcessAuthenticationResponse(msg);
		}
	}

	public string GetAuthToken()
	{
		if (this.AuthenticationResponse.SignedAuthToken != null)
		{
			return JsonSerialize.ToJson(this.AuthenticationResponse.SignedAuthToken);
		}
		return null;
	}

	[Server]
	private void RequestAuthentication()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::RequestAuthentication()' called when server was not active");
			return;
		}
		if (!base.isLocalPlayer)
		{
			this._authenticationRequested = true;
			this._hub.encryptedChannelManager.PrepareExchange();
		}
		this._challenge = RandomGenerator.GetStringSecure(24);
		this.RpcRequestAuthentication(this._challenge, (this._hub.encryptedChannelManager.EcdhKeys == null) ? null : ECDSA.KeyToString(this._hub.encryptedChannelManager.EcdhKeys.Public));
	}

	[TargetRpc]
	private void RpcRequestAuthentication(string challenge, string ecdhPublicKey)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(challenge);
		writer.WriteString(ecdhPublicKey);
		this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcRequestAuthentication(System.String,System.String)", -1619731460, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	internal void TargetSetRealId(NetworkConnection conn, string userId)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(userId);
		this.SendTargetRPCInternal(conn, "System.Void CentralAuth.PlayerAuthenticationManager::TargetSetRealId(Mirror.NetworkConnection,System.String)", 295172299, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	private void ProcessAuthenticationResponse(AuthenticationResponse msg)
	{
		try
		{
			if (!this._authenticationRequested)
			{
				this._hub.gameConsoleTransmission.SendToClient("Authentication token was not requested by the server.", "yellow");
				return;
			}
			if (this._challenge == null)
			{
				this._hub.gameConsoleTransmission.SendToClient("Authentication token has already been sent.", "yellow");
				return;
			}
			this.AuthenticationResponse = msg;
			if (msg.SignedAuthToken != null)
			{
				AuthenticationToken token;
				string error;
				string userId;
				if ((msg.EcdhPublicKey == null || msg.EcdhPublicKeySignature == null) && !base.isLocalPlayer)
				{
					this.RejectAuthentication("null ECDH public key or public key signature.");
				}
				else if (msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out token, out error, out userId))
				{
					string text = PlayerAuthenticationManager.RemoveSalt(token.UserId);
					if (this._challenge != token.Challenge)
					{
						this.RejectAuthentication("invalid authentication challenge.", userId);
						return;
					}
					this._challenge = null;
					if (token.PublicKey != msg.PublicKeyHash)
					{
						this.RejectAuthentication("public key hash mismatch.", userId);
						return;
					}
					if (GameCore.Version.PrivateBeta && !token.PrivateBetaOwnership)
					{
						this.RejectAuthentication("you don't own the Private Beta Access Pass DLC.", userId);
						return;
					}
					IPEndPoint iPEndPoint = null;
					if (!base.isLocalPlayer)
					{
						iPEndPoint = LiteNetLib4MirrorServer.Peers[base.connectionToClient.connectionId].EndPoint;
						if (iPEndPoint != null && (!CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(iPEndPoint) || !CustomLiteNetLib4MirrorTransport.UserIds[iPEndPoint].UserId.Equals(text, StringComparison.Ordinal)) && !CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(text))
						{
							this._hub.gameConsoleTransmission.SendToClient("UserID mismatch between authentication and preauthentication token.", "red");
							this._hub.gameConsoleTransmission.SendToClient("Preauth: " + (CustomLiteNetLib4MirrorTransport.UserIds.TryGetValue(iPEndPoint, out var value) ? value.UserId : "(null)"), "red");
							this._hub.gameConsoleTransmission.SendToClient("Auth: " + text, "red");
							this.RejectAuthentication("UserID mismatch between authentication and preauthentication token. Check the game console for more details.", text);
							return;
						}
						if (iPEndPoint != null && CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(iPEndPoint))
						{
							CustomLiteNetLib4MirrorTransport.UserIds.Remove(iPEndPoint);
						}
					}
					if (CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(text))
					{
						CustomLiteNetLib4MirrorTransport.UserIdFastReload.Remove(text);
					}
					if (msg.EcdhPublicKey != null && !ECDSA.VerifyBytes(msg.EcdhPublicKey, msg.EcdhPublicKeySignature, msg.PublicKey))
					{
						this.RejectAuthentication("invalid ECDH exchange public key signature.", userId);
					}
					else
					{
						if (!this.CheckBans(token, text))
						{
							return;
						}
						if (msg.EcdhPublicKey != null)
						{
							this._hub.encryptedChannelManager.ServerProcessExchange(msg.EcdhPublicKey);
						}
						msg.AuthToken = token;
						this.AuthenticationResponse = msg;
						string text2 = string.Format("{0} authenticated from endpoint {1}. Player ID assigned: {2}. Auth token serial number: {3}.", PlayerAuthenticationManager.RemoveSalt(msg.AuthToken.UserId), (iPEndPoint == null) ? "(null)" : iPEndPoint.ToString(), this._hub.PlayerId, msg.AuthToken.Serial);
						ServerConsole.AddLog(text2);
						ServerLogs.AddLog(ServerLogs.Modules.Networking, text2, ServerLogs.ServerLogType.ConnectionUpdate);
						this.FinalizeAuthentication();
						if (msg.SignedBadgeToken != null)
						{
							if (msg.SignedBadgeToken.TryGetToken<BadgeToken>("Badge request", out var token2, out var error2, out var _))
							{
								if (token2.Serial != this.AuthenticationResponse.AuthToken.Serial)
								{
									this.RejectAuthentication("token serial number mismatch.");
									return;
								}
								if (token2.UserId != Sha.HashToString(Sha.Sha512(this.SaltedUserId)))
								{
									this.RejectBadgeToken("badge token UserID mismatch.");
									return;
								}
								if (StringUtils.Base64Decode(token2.Nickname) != this._hub.nicknameSync.MyNick)
								{
									this.RejectBadgeToken("badge token nickname mismatch.");
									return;
								}
								msg.BadgeToken = token2;
								this.AuthenticationResponse = msg;
								ulong num = ((token2.RaPermissions == 0L || ServerStatic.PermissionsHandler.NorthwoodAccess) ? ServerStatic.PermissionsHandler.FullPerm : token2.RaPermissions);
								if ((token2.Management || token2.GlobalBanning) && CustomNetworkManager.IsVerified)
								{
									this._hub.serverRoles.GlobalPerms |= 8388608uL;
									this._hub.serverRoles.GlobalPerms |= 1048576uL;
								}
								if (this.AuthenticationResponse.BadgeToken.OverwatchMode)
								{
									this._hub.serverRoles.GlobalPerms |= 4096uL;
								}
								if (token2.Staff || token2.Management || token2.GlobalBanning)
								{
									this._hub.serverRoles.GlobalPerms |= 16908288uL;
								}
								if ((token2.Staff && ServerStatic.PermissionsHandler.NorthwoodAccess) || (token2.RemoteAdmin && ServerStatic.PermissionsHandler.StaffAccess) || (token2.Management && ServerStatic.PermissionsHandler.ManagersAccess) || (token2.GlobalBanning && ServerStatic.PermissionsHandler.BanningTeamAccess))
								{
									this._hub.serverRoles.GlobalPerms |= num;
								}
								if ((token2.BadgeText != null && token2.BadgeText != "(none)") || (token2.BadgeColor != null && token2.BadgeColor != "(none)"))
								{
									if (this._hub.serverRoles.UserBadgePreferences == ServerRoles.BadgePreferences.PreferGlobal || !this._hub.serverRoles.BadgeCover || this._hub.serverRoles.Group == null)
									{
										bool flag = msg.HideBadge;
										switch (token2.BadgeType)
										{
										case 1:
											if (!ConfigFile.ServerConfig.GetBool("hide_staff_badges_by_default"))
											{
												break;
											}
											goto case 3;
										case 2:
											if (!ConfigFile.ServerConfig.GetBool("hide_management_badges_by_default"))
											{
												break;
											}
											goto case 3;
										case 0:
											if (!ConfigFile.ServerConfig.GetBool("hide_patreon_badges_by_default") || CustomNetworkManager.IsVerified)
											{
												break;
											}
											goto case 3;
										case 3:
											flag = true;
											break;
										}
										if (flag)
										{
											this._hub.serverRoles.HiddenBadge = token2.BadgeText;
											this._hub.serverRoles.GlobalHidden = true;
											this._hub.serverRoles.RefreshHiddenTag();
											this._hub.gameConsoleTransmission.SendToClient("Your global badge has been granted, but it's hidden. Use \".gtag\" command in the game console to show your global badge.", "yellow");
										}
										else
										{
											this._hub.serverRoles.HiddenBadge = null;
											this._hub.serverRoles.RpcResetFixed();
											this._hub.serverRoles.NetworkGlobalBadge = this.AuthenticationResponse.SignedBadgeToken.token;
											this._hub.serverRoles.NetworkGlobalBadgeSignature = this.AuthenticationResponse.SignedBadgeToken.signature;
											this._hub.gameConsoleTransmission.SendToClient("Your global badge has been granted.", "cyan");
										}
									}
									else
									{
										this._hub.gameConsoleTransmission.SendToClient("Your global badge is covered by server badge. Use \".gtag\" command in the game console to show your global badge.", "yellow");
									}
								}
								this._hub.serverRoles.FinalizeSetGroup();
							}
							else
							{
								this.RejectBadgeToken(error2);
							}
						}
						PlayerEvents.OnJoined(new PlayerJoinedEventArgs(this._hub));
					}
				}
				else
				{
					this.RejectAuthentication(error, userId, removeSalt: true);
				}
			}
			else
			{
				this.RejectAuthentication("authentication token not provided.");
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Exception during authentication (client address: " + base.connectionToClient.address + "): " + ex.Message, ConsoleColor.Magenta);
			ServerConsole.AddLog(ex.StackTrace, ConsoleColor.Magenta);
			this.RejectAuthentication("server exception during authentication!");
		}
	}

	[Server]
	private void RejectAuthentication(string reason, string userId = null, bool removeSalt = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::RejectAuthentication(System.String,System.String,System.Boolean)' called when server was not active");
			return;
		}
		if (userId != null && removeSalt)
		{
			userId = PlayerAuthenticationManager.RemoveSalt(userId);
		}
		ServerConsole.AddLog("Player " + (userId ?? "(unknown)") + " (" + base.connectionToClient.address + ") failed to authenticate: " + reason);
		this._hub.gameConsoleTransmission.SendToClient("Authentication failure: " + reason, "red");
		ServerConsole.Disconnect(this._hub.connectionToClient, "Authentication failure: " + reason);
	}

	[Server]
	private void RejectBadgeToken(string reason)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::RejectBadgeToken(System.String)' called when server was not active");
		}
		else
		{
			this._hub.gameConsoleTransmission.SendToClient("Your global badge token is invalid. Reason: " + reason, "red");
		}
	}

	[Server]
	private void FinalizeAuthentication()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::FinalizeAuthentication()' called when server was not active");
			return;
		}
		this.UserId = this.AuthenticationResponse.AuthToken.UserId;
		this.DoNotTrack = this.AuthenticationResponse.DoNotTrack || this.AuthenticationResponse.AuthToken.DoNotTrack;
		this._hub.nicknameSync.UpdateNickname(StringUtils.Base64Decode(this.AuthenticationResponse.AuthToken.Nickname));
		if (this.DoNotTrack)
		{
			ServerLogs.AddLog(ServerLogs.Modules.Networking, this._hub.LoggedNameFromRefHub() + " connected from IP address " + base.connectionToClient.address + " sent Do Not Track signal.", ServerLogs.ServerLogType.ConnectionUpdate);
		}
		this._hub.gameConsoleTransmission.SendToClient("Hi " + this._hub.nicknameSync.MyNick + "! You have been authenticated on this server.", "green");
		this._hub.serverRoles.RefreshPermissions();
		if (PlayerAuthenticationManager.AllowSameAccountJoining)
		{
			return;
		}
		int playerId = ReferenceHub.GetHub(base.gameObject).PlayerId;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.authManager.UserId == this.UserId && allHub.PlayerId != playerId && !allHub.isLocalPlayer)
			{
				ServerConsole.AddLog($"Player {this.UserId} ({allHub.PlayerId}, {base.connectionToClient.address}) has been kicked from the server, because he has just joined the server again from IP address {base.connectionToClient.address}.");
				ServerConsole.Disconnect(allHub.gameObject, "Only one player instance of the same player is allowed.");
			}
		}
	}

	[Server]
	private bool CheckBans(AuthenticationToken token, string unsalted)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean CentralAuth.PlayerAuthenticationManager::CheckBans(AuthenticationToken,System.String)' called when server was not active");
			return default(bool);
		}
		if ((!token.BypassBans || !CustomNetworkManager.IsVerified) && BanHandler.QueryBan(unsalted, null).Key != null)
		{
			this._hub.gameConsoleTransmission.SendToClient("You are banned from this server.", "red");
			ServerConsole.AddLog("Player kicked due to local UserID ban.");
			ServerConsole.Disconnect(this._hub.connectionToClient, "You are banned from this server.");
			return false;
		}
		if (CustomNetworkManager.IsVerified || CustomLiteNetLib4MirrorTransport.UseGlobalBans)
		{
			switch (token.GlobalBan)
			{
			case "M1":
				ServerConsole.AddLog("Player " + token.UserId + " is globally muted.");
				break;
			case "M2":
				ServerConsole.AddLog("Player " + token.UserId + " is globally muted on intercom.");
				break;
			default:
				this._hub.gameConsoleTransmission.SendToClient(token.GlobalBan, "red");
				ServerConsole.AddLog("Player " + token.UserId + " has been kicked due to an active global ban: " + token.GlobalBan);
				ServerConsole.Disconnect(this._hub.connectionToClient, token.GlobalBan);
				return false;
			case "NO":
				break;
			}
		}
		if (!token.SkipIpCheck && !token.RequestIp.Equals("N/A", StringComparison.Ordinal) && ServerConsole.EnforceSameIp)
		{
			string address = this._hub.connectionToClient.address;
			if ((address.Contains(".", StringComparison.Ordinal) && token.RequestIp.Contains(".", StringComparison.Ordinal)) || (address.Contains(":", StringComparison.Ordinal) && token.RequestIp.Contains(":", StringComparison.Ordinal)))
			{
				bool flag = false;
				if (ServerConsole.SkipEnforcementForLocalAddresses)
				{
					flag = address == "127.0.0.1" || address.StartsWith("10.", StringComparison.Ordinal) || address.StartsWith("192.168.", StringComparison.Ordinal);
					if (!flag && address.StartsWith("172.", StringComparison.Ordinal))
					{
						string[] array = address.Split('.');
						if (array.Length == 4 && byte.TryParse(array[1], out var result) && result >= 16 && result <= 31)
						{
							flag = true;
						}
					}
				}
				if (!flag && address != token.RequestIp)
				{
					this._hub.gameConsoleTransmission.SendToClient("Authentication token has been issued to a different IP address.", "red");
					this._hub.gameConsoleTransmission.SendToClient("Your IP address: " + address, "red");
					this._hub.gameConsoleTransmission.SendToClient("Issued to: " + token.RequestIp, "red");
					ServerConsole.AddLog("Player kicked due to IP addresses mismatch.");
					ServerConsole.Disconnect(this._hub.connectionToClient, "Authentication token has been issued to a different IP address. You can find details in the game console.");
					return false;
				}
			}
		}
		VcMuteFlags vcMuteFlags = VcMuteFlags.None;
		if (VoiceChatMutes.QueryLocalMute(unsalted))
		{
			vcMuteFlags |= VcMuteFlags.LocalRegular;
			this._hub.gameConsoleTransmission.SendToClient("You are muted on the voice chat by the server administrator.", "red");
		}
		if ((ConfigFile.ServerConfig.GetBool("global_mutes_voicechat", def: true) || CustomNetworkManager.IsVerified) && token.GlobalBan == "M1")
		{
			vcMuteFlags |= VcMuteFlags.GlobalRegular;
			this._hub.gameConsoleTransmission.SendToClient("You are globally muted on the voice chat.", "red");
		}
		if (VoiceChatMutes.QueryLocalMute(unsalted, intercom: true))
		{
			vcMuteFlags |= VcMuteFlags.LocalIntercom;
			this._hub.gameConsoleTransmission.SendToClient("You are muted on the intercom by the server administrator.", "red");
		}
		else if ((ConfigFile.ServerConfig.GetBool("global_mutes_intercom", def: true) || CustomNetworkManager.IsVerified) && token.GlobalBan == "M2")
		{
			vcMuteFlags |= VcMuteFlags.GlobalIntercom;
			this._hub.gameConsoleTransmission.SendToClient("You are globally muted on the intercom.", "red");
		}
		if (token.BypassBans)
		{
			vcMuteFlags = VcMuteFlags.None;
		}
		VoiceChatMutes.SetFlags(this._hub, vcMuteFlags);
		return true;
	}

	private static string RemoveSalt(string userId)
	{
		if (userId != null)
		{
			if (userId.Contains("$"))
			{
				return userId.Substring(0, userId.IndexOf("$", StringComparison.Ordinal));
			}
			return userId;
		}
		return null;
	}

	[Command]
	private void CmdHandlePasswordAuthentication(string clientSalt, byte[] signature, string ecdhPublicKey)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(clientSalt);
		writer.WriteBytesAndSize(signature);
		writer.WriteString(ecdhPublicKey);
		base.SendCommandInternal("System.Void CentralAuth.PlayerAuthenticationManager::CmdHandlePasswordAuthentication(System.String,System.Byte[],System.String)", 1988415082, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	private void RpcFinishExchange(string publicKey, byte[] signature)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(publicKey);
		writer.WriteBytesAndSize(signature);
		this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcFinishExchange(System.String,System.Byte[])", 112993832, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	private void RpcAnimateInvalidPassword()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcAnimateInvalidPassword()", 2027815992, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	public void ResetPasswordAttempts()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::ResetPasswordAttempts()' called when server was not active");
		}
		else
		{
			this._passwordAttempts = 0u;
		}
	}

	[Server]
	private void AssignPasswordOverrideGroup()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::AssignPasswordOverrideGroup()' called when server was not active");
			return;
		}
		UserGroup overrideGroup = ServerStatic.PermissionsHandler.OverrideGroup;
		if (overrideGroup != null)
		{
			string text = this._hub.LoggedNameFromRefHub() + " used a valid RemoteAdmin override password.";
			ServerConsole.AddLog(text, ConsoleColor.DarkYellow);
			ServerLogs.AddLog(ServerLogs.Modules.Permissions, text, ServerLogs.ServerLogType.ConnectionUpdate);
			this._hub.serverRoles.SetGroup(overrideGroup, byAdmin: true);
		}
		else
		{
			ServerConsole.AddLog("Non-existing group is assigned for override password!", ConsoleColor.Red);
			this._hub.gameConsoleTransmission.SendToClient("Non-existing group is assigned for override password!", "red");
		}
	}

	private static byte[] DerivePassword(string password, string serversalt, string clientsalt)
	{
		byte[] salt = Sha.Sha512(serversalt + "/" + clientsalt);
		return PBKDF2.Pbkdf2HashBytes(password, salt, 1600, 512);
	}

	private void UserIdHook(string p, string i)
	{
		PlayerAuthenticationManager.OnSyncedUserIdAssigned?.Invoke(this._hub);
		if (string.IsNullOrEmpty(i))
		{
			this.InstanceMode = ClientInstanceMode.Unverified;
			return;
		}
		this.InstanceMode = i switch
		{
			"ID_Dedicated" => ClientInstanceMode.DedicatedServer, 
			"ID_Host" => ClientInstanceMode.Host, 
			"ID_Dummy" => ClientInstanceMode.Dummy, 
			_ => ClientInstanceMode.ReadyClient, 
		};
	}

	static PlayerAuthenticationManager()
	{
		PlayerAuthenticationManager._saltRegex = new Regex("^[a-zA-Z0-9]{32}$", RegexOptions.Compiled);
		RemoteProcedureCalls.RegisterCommand(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::CmdHandlePasswordAuthentication(System.String,System.Byte[],System.String)", InvokeUserCode_CmdHandlePasswordAuthentication__String__Byte_005B_005D__String, requiresAuthority: true);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcRequestAuthentication(System.String,System.String)", InvokeUserCode_RpcRequestAuthentication__String__String);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::TargetSetRealId(Mirror.NetworkConnection,System.String)", InvokeUserCode_TargetSetRealId__NetworkConnection__String);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcFinishExchange(System.String,System.Byte[])", InvokeUserCode_RpcFinishExchange__String__Byte_005B_005D);
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcAnimateInvalidPassword()", InvokeUserCode_RpcAnimateInvalidPassword);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcRequestAuthentication__String__String(string challenge, string ecdhPublicKey)
	{
	}

	protected static void InvokeUserCode_RpcRequestAuthentication__String__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC RpcRequestAuthentication called on server.");
		}
		else
		{
			((PlayerAuthenticationManager)obj).UserCode_RpcRequestAuthentication__String__String(reader.ReadString(), reader.ReadString());
		}
	}

	protected void UserCode_TargetSetRealId__NetworkConnection__String(NetworkConnection conn, string userId)
	{
	}

	protected static void InvokeUserCode_TargetSetRealId__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetRealId called on server.");
		}
		else
		{
			((PlayerAuthenticationManager)obj).UserCode_TargetSetRealId__NetworkConnection__String(null, reader.ReadString());
		}
	}

	protected void UserCode_CmdHandlePasswordAuthentication__String__Byte_005B_005D__String(string clientSalt, byte[] signature, string ecdhPublicKey)
	{
		if (this._passwordAttempts > 2)
		{
			this._hub.gameConsoleTransmission.SendToClient("Limit of RA password auth attempts exceeded.", "red");
			return;
		}
		if (this._passwordCooldown > 0f)
		{
			this._hub.gameConsoleTransmission.SendToClient("Please wait before trying to use password again!", "red");
			return;
		}
		this._passwordCooldown = 1.8f;
		if (base.isLocalPlayer)
		{
			this._hub.gameConsoleTransmission.SendToClient("Password authentication is not available for the host.", "red");
			return;
		}
		ReferenceHub hostHub = ReferenceHub.HostHub;
		if (!hostHub.queryProcessor.OverridePasswordEnabled)
		{
			this._hub.gameConsoleTransmission.SendToClient("Password authentication is disabled on this server!", "red");
			return;
		}
		if (clientSalt == null || signature == null)
		{
			this._hub.gameConsoleTransmission.SendToClient("Invalid password auth request - null parameters.", "red");
			return;
		}
		if (!PlayerAuthenticationManager._saltRegex.IsMatch(clientSalt))
		{
			this._hub.gameConsoleTransmission.SendToClient("Invalid password auth request - invalid client salt.", "red");
			return;
		}
		byte[] key = PlayerAuthenticationManager.DerivePassword(ServerStatic.PermissionsHandler.OverridePassword, hostHub.encryptedChannelManager.ServerRandom, clientSalt);
		if (PlayerAuthenticationManager.OnlineMode)
		{
			if (this.SyncedUserId == null)
			{
				this._hub.gameConsoleTransmission.SendToClient("Can't process password auth request - not authenticated while server is in online mode.", "red");
			}
			else if (!Sha.Sha512Hmac(key, this.SyncedUserId).SequenceEqual(signature))
			{
				this._passwordAttempts++;
				string text = this._hub.LoggedNameFromRefHub() + " attempted to use an invalid RemoteAdmin override password.";
				ServerConsole.AddLog(text, ConsoleColor.Magenta);
				ServerLogs.AddLog(ServerLogs.Modules.Permissions, text, ServerLogs.ServerLogType.ConnectionUpdate);
				this.RpcAnimateInvalidPassword();
			}
			else
			{
				this.AssignPasswordOverrideGroup();
			}
		}
		else if (ecdhPublicKey == null)
		{
			this._hub.gameConsoleTransmission.SendToClient("Can't process password auth request - ecdhPublicKey is null in offline mode.", "red");
		}
		else if (!Sha.Sha512Hmac(key, ecdhPublicKey).SequenceEqual(signature))
		{
			this._passwordAttempts++;
			string text2 = this._hub.LoggedNameFromRefHub() + " attempted to use an invalid RemoteAdmin override password.";
			ServerConsole.AddLog(text2, ConsoleColor.Magenta);
			ServerLogs.AddLog(ServerLogs.Modules.Permissions, text2, ServerLogs.ServerLogType.ConnectionUpdate);
			this.RpcAnimateInvalidPassword();
		}
		else
		{
			this._hub.encryptedChannelManager.PrepareExchange();
			this._hub.encryptedChannelManager.ServerProcessExchange(ecdhPublicKey);
			string text3 = ECDSA.KeyToString(this._hub.encryptedChannelManager.EcdhKeys.Public);
			byte[] signature2 = Sha.Sha512Hmac(key, text3);
			this.RpcFinishExchange(text3, signature2);
			this.AssignPasswordOverrideGroup();
		}
	}

	protected static void InvokeUserCode_CmdHandlePasswordAuthentication__String__Byte_005B_005D__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdHandlePasswordAuthentication called on client.");
		}
		else
		{
			((PlayerAuthenticationManager)obj).UserCode_CmdHandlePasswordAuthentication__String__Byte_005B_005D__String(reader.ReadString(), reader.ReadBytesAndSize(), reader.ReadString());
		}
	}

	protected void UserCode_RpcFinishExchange__String__Byte_005B_005D(string publicKey, byte[] signature)
	{
	}

	protected static void InvokeUserCode_RpcFinishExchange__String__Byte_005B_005D(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC RpcFinishExchange called on server.");
		}
		else
		{
			((PlayerAuthenticationManager)obj).UserCode_RpcFinishExchange__String__Byte_005B_005D(reader.ReadString(), reader.ReadBytesAndSize());
		}
	}

	protected void UserCode_RpcAnimateInvalidPassword()
	{
	}

	protected static void InvokeUserCode_RpcAnimateInvalidPassword(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC RpcAnimateInvalidPassword called on server.");
		}
		else
		{
			((PlayerAuthenticationManager)obj).UserCode_RpcAnimateInvalidPassword();
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteString(this.SyncedUserId);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteString(this.SyncedUserId);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.SyncedUserId, UserIdHook, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SyncedUserId, UserIdHook, reader.ReadString());
		}
	}
}
