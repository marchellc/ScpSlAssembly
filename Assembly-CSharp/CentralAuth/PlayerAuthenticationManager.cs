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
using NetworkManagerUtils;
using NorthwoodLib;
using UnityEngine;
using VoiceChat;

namespace CentralAuth
{
	public class PlayerAuthenticationManager : NetworkBehaviour
	{
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
					string privUserId = this._privUserId;
					int num = this._privUserId.IndexOf("$", StringComparison.Ordinal) - 0;
					return privUserId.Substring(0, num);
				}
				return this._privUserId;
			}
			set
			{
				if (!NetworkServer.active)
				{
					return;
				}
				this._privUserId = value;
				this.UserIdHook(null, value);
				this.RefreshSyncedId();
				this._hub.serverRoles.RefreshRealId();
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
			this.NetworkSyncedUserId = ((base.isLocalPlayer || this._hub.IsDummy || (this._privUserId.EndsWith("@steam", StringComparison.Ordinal) && !this.DoNotTrack && !this.AuthenticationResponse.AuthToken.SyncHashed)) ? this._privUserId : Sha.HashToString(Sha.Sha512(this._privUserId)));
		}

		public ClientInstanceMode InstanceMode
		{
			get
			{
				return this._targetInstanceMode;
			}
			private set
			{
				if (value == this._targetInstanceMode)
				{
					return;
				}
				this._targetInstanceMode = value;
				Action<ReferenceHub, ClientInstanceMode> onInstanceModeChanged = PlayerAuthenticationManager.OnInstanceModeChanged;
				if (onInstanceModeChanged == null)
				{
					return;
				}
				onInstanceModeChanged(this._hub, this._targetInstanceMode);
			}
		}

		public static event Action<ReferenceHub> OnSyncedUserIdAssigned;

		public static event Action<ReferenceHub, ClientInstanceMode> OnInstanceModeChanged;

		public bool NorthwoodStaff
		{
			get
			{
				BadgeToken badgeToken = this.AuthenticationResponse.BadgeToken;
				return badgeToken != null && badgeToken.Staff;
			}
		}

		public bool BypassBansFlagSet
		{
			get
			{
				AuthenticationToken authToken = this.AuthenticationResponse.AuthToken;
				return authToken != null && authToken.BypassBans;
			}
		}

		public bool RemoteAdminGlobalAccess
		{
			get
			{
				BadgeToken badgeToken = this.AuthenticationResponse.BadgeToken;
				return badgeToken != null && (badgeToken.Management || badgeToken.GlobalBanning);
			}
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
			if (this.InstanceMode != ClientInstanceMode.Unverified || !NetworkServer.active || !PlayerAuthenticationManager.OnlineMode || base.isLocalPlayer || this._timeoutTimer < 0f)
			{
				return;
			}
			if (!this._authenticationRequested && base.connectionToClient.isReady)
			{
				this.RequestAuthentication();
			}
			this._timeoutTimer += Time.fixedDeltaTime;
			if (this._timeoutTimer <= PlayerAuthenticationManager.AuthenticationTimeout)
			{
				return;
			}
			this._timeoutTimer = -1f;
			this.RejectAuthentication("authentication timeout exceeded.", null, false);
		}

		private void Start()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (base.isLocalPlayer)
			{
				NetworkServer.ReplaceHandler<AuthenticationResponse>(new Action<NetworkConnectionToClient, AuthenticationResponse>(PlayerAuthenticationManager.ServerReceiveAuthenticationResponse), true);
			}
			if (base.connectionToClient is DummyNetworkConnection)
			{
				this.UserId = "ID_Dummy";
				return;
			}
			if (base.isLocalPlayer && ServerStatic.IsDedicated)
			{
				this.UserId = "ID_Dedicated";
				return;
			}
			if (base.isLocalPlayer)
			{
				this.UserId = "ID_Host";
				if (PlayerAuthenticationManager.OnlineMode)
				{
					this.RequestAuthentication();
					return;
				}
			}
			else if (!PlayerAuthenticationManager.OnlineMode)
			{
				this.UserId = "ID_Offline_" + base.netId.ToString() + "_" + DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
			}
		}

		private static void ServerReceiveAuthenticationResponse(NetworkConnection conn, AuthenticationResponse msg)
		{
			ReferenceHub referenceHub;
			if (!NetworkServer.active || !PlayerAuthenticationManager.OnlineMode || !ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			referenceHub.authManager.ProcessAuthenticationResponse(msg);
		}

		public string GetAuthToken()
		{
			if (this.AuthenticationResponse.SignedAuthToken != null)
			{
				return JsonSerialize.ToJson<SignedToken>(this.AuthenticationResponse.SignedAuthToken);
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
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(challenge);
			networkWriterPooled.WriteString(ecdhPublicKey);
			this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcRequestAuthentication(System.String,System.String)", -1619731460, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[TargetRpc]
		internal void TargetSetRealId(NetworkConnection conn, string userId)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(userId);
			this.SendTargetRPCInternal(conn, "System.Void CentralAuth.PlayerAuthenticationManager::TargetSetRealId(Mirror.NetworkConnection,System.String)", 295172299, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void ProcessAuthenticationResponse(AuthenticationResponse msg)
		{
			try
			{
				this.AuthenticationResponse = msg;
				if (msg.SignedAuthToken != null)
				{
					AuthenticationToken authenticationToken;
					string text;
					string text2;
					if ((msg.EcdhPublicKey == null || msg.EcdhPublicKeySignature == null) && !base.isLocalPlayer)
					{
						this.RejectAuthentication("null ECDH public key or public key signature.", null, false);
					}
					else if (msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out authenticationToken, out text, out text2, 0))
					{
						string text3 = PlayerAuthenticationManager.RemoveSalt(authenticationToken.UserId);
						if (this._challenge != authenticationToken.Challenge)
						{
							this.RejectAuthentication("invalid authentication challenge.", text2, false);
						}
						else
						{
							this._challenge = null;
							if (authenticationToken.PublicKey != msg.PublicKeyHash)
							{
								this.RejectAuthentication("public key hash mismatch.", text2, false);
							}
							else if (global::GameCore.Version.PrivateBeta && !authenticationToken.PrivateBetaOwnership)
							{
								this.RejectAuthentication("you don't own the Private Beta Access Pass DLC.", text2, false);
							}
							else
							{
								IPEndPoint ipendPoint = null;
								if (!base.isLocalPlayer)
								{
									ipendPoint = LiteNetLib4MirrorServer.Peers[base.connectionToClient.connectionId].EndPoint;
									if (ipendPoint != null && (!CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(ipendPoint) || !CustomLiteNetLib4MirrorTransport.UserIds[ipendPoint].UserId.Equals(text3, StringComparison.Ordinal)) && !CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(text3))
									{
										this._hub.gameConsoleTransmission.SendToClient("UserID mismatch between authentication and preauthentication token.", "red");
										PreauthItem preauthItem;
										this._hub.gameConsoleTransmission.SendToClient("Preauth: " + (CustomLiteNetLib4MirrorTransport.UserIds.TryGetValue(ipendPoint, out preauthItem) ? preauthItem.UserId : "(null)"), "red");
										this._hub.gameConsoleTransmission.SendToClient("Auth: " + text3, "red");
										this.RejectAuthentication("UserID mismatch between authentication and preauthentication token. Check the game console for more details.", text3, false);
										return;
									}
									if (ipendPoint != null && CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(ipendPoint))
									{
										CustomLiteNetLib4MirrorTransport.UserIds.Remove(ipendPoint);
									}
								}
								if (CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(text3))
								{
									CustomLiteNetLib4MirrorTransport.UserIdFastReload.Remove(text3);
								}
								if (msg.EcdhPublicKey != null && !ECDSA.VerifyBytes(msg.EcdhPublicKey, msg.EcdhPublicKeySignature, msg.PublicKey))
								{
									this.RejectAuthentication("invalid ECDH exchange public key signature.", text2, false);
								}
								else if (this.CheckBans(authenticationToken, text3))
								{
									if (msg.EcdhPublicKey != null)
									{
										this._hub.encryptedChannelManager.ServerProcessExchange(msg.EcdhPublicKey);
									}
									msg.AuthToken = authenticationToken;
									this.AuthenticationResponse = msg;
									string text4 = string.Format("{0} authenticated from endpoint {1}. Player ID assigned: {2}. Auth token serial number: {3}.", new object[]
									{
										PlayerAuthenticationManager.RemoveSalt(msg.AuthToken.UserId),
										(ipendPoint == null) ? "(null)" : ipendPoint.ToString(),
										this._hub.PlayerId,
										msg.AuthToken.Serial
									});
									ServerConsole.AddLog(text4, ConsoleColor.Gray, false);
									ServerLogs.AddLog(ServerLogs.Modules.Networking, text4, ServerLogs.ServerLogType.ConnectionUpdate, false);
									this.FinalizeAuthentication();
									if (msg.SignedBadgeToken != null)
									{
										BadgeToken badgeToken;
										string text5;
										string text6;
										if (msg.SignedBadgeToken.TryGetToken<BadgeToken>("Badge request", out badgeToken, out text5, out text6, 0))
										{
											if (badgeToken.Serial != this.AuthenticationResponse.AuthToken.Serial)
											{
												this.RejectAuthentication("token serial number mismatch.", null, false);
												return;
											}
											if (badgeToken.UserId != Sha.HashToString(Sha.Sha512(this.SaltedUserId)))
											{
												this.RejectBadgeToken("badge token UserID mismatch.");
												return;
											}
											if (StringUtils.Base64Decode(badgeToken.Nickname) != this._hub.nicknameSync.MyNick)
											{
												this.RejectBadgeToken("badge token nickname mismatch.");
												return;
											}
											msg.BadgeToken = badgeToken;
											this.AuthenticationResponse = msg;
											ulong num = ((badgeToken.RaPermissions == 0UL || ServerStatic.PermissionsHandler.NorthwoodAccess) ? ServerStatic.PermissionsHandler.FullPerm : badgeToken.RaPermissions);
											if ((badgeToken.Management || badgeToken.GlobalBanning) && CustomNetworkManager.IsVerified)
											{
												this._hub.serverRoles.GlobalPerms |= 8388608UL;
												this._hub.serverRoles.GlobalPerms |= 1048576UL;
											}
											if (this.AuthenticationResponse.BadgeToken.OverwatchMode)
											{
												this._hub.serverRoles.GlobalPerms |= 4096UL;
											}
											if (badgeToken.Staff || badgeToken.Management || badgeToken.GlobalBanning)
											{
												this._hub.serverRoles.GlobalPerms |= 16908288UL;
											}
											if ((badgeToken.Staff && ServerStatic.PermissionsHandler.NorthwoodAccess) || (badgeToken.RemoteAdmin && ServerStatic.PermissionsHandler.StaffAccess) || (badgeToken.Management && ServerStatic.PermissionsHandler.ManagersAccess) || (badgeToken.GlobalBanning && ServerStatic.PermissionsHandler.BanningTeamAccess))
											{
												this._hub.serverRoles.GlobalPerms |= num;
											}
											if ((badgeToken.BadgeText != null && badgeToken.BadgeText != "(none)") || (badgeToken.BadgeColor != null && badgeToken.BadgeColor != "(none)"))
											{
												if (this._hub.serverRoles.UserBadgePreferences == ServerRoles.BadgePreferences.PreferGlobal || !this._hub.serverRoles.BadgeCover || this._hub.serverRoles.Group == null)
												{
													bool flag = msg.HideBadge;
													switch (badgeToken.BadgeType)
													{
													case 0:
														if (!ConfigFile.ServerConfig.GetBool("hide_patreon_badges_by_default", false) || CustomNetworkManager.IsVerified)
														{
															goto IL_0611;
														}
														break;
													case 1:
														if (!ConfigFile.ServerConfig.GetBool("hide_staff_badges_by_default", false))
														{
															goto IL_0611;
														}
														break;
													case 2:
														if (!ConfigFile.ServerConfig.GetBool("hide_management_badges_by_default", false))
														{
															goto IL_0611;
														}
														break;
													case 3:
														break;
													default:
														goto IL_0611;
													}
													flag = true;
													IL_0611:
													if (flag)
													{
														this._hub.serverRoles.HiddenBadge = badgeToken.BadgeText;
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
											this.RejectBadgeToken(text5);
										}
									}
									PlayerEvents.OnJoined(new PlayerJoinedEventArgs(this._hub));
								}
							}
						}
					}
					else
					{
						this.RejectAuthentication(text, text2, true);
					}
				}
				else
				{
					this.RejectAuthentication("authentication token not provided.", null, false);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Exception during authentication (client address: " + base.connectionToClient.address + "): " + ex.Message, ConsoleColor.Magenta, false);
				ServerConsole.AddLog(ex.StackTrace, ConsoleColor.Magenta, false);
				this.RejectAuthentication("server exception during authentication!", null, false);
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
			ServerConsole.AddLog(string.Concat(new string[]
			{
				"Player ",
				userId ?? "(unknown)",
				" (",
				base.connectionToClient.address,
				") failed to authenticate: ",
				reason
			}), ConsoleColor.Gray, false);
			this._hub.gameConsoleTransmission.SendToClient("Authentication failure: " + reason, "red");
			ServerConsole.Disconnect(this._hub.connectionToClient, "Authentication failure: " + reason);
		}

		[Server]
		private void RejectBadgeToken(string reason)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::RejectBadgeToken(System.String)' called when server was not active");
				return;
			}
			this._hub.gameConsoleTransmission.SendToClient("Your global badge token is invalid. Reason: " + reason, "red");
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
				ServerLogs.AddLog(ServerLogs.Modules.Networking, this._hub.LoggedNameFromRefHub() + " connected from IP address " + base.connectionToClient.address + " sent Do Not Track signal.", ServerLogs.ServerLogType.ConnectionUpdate, false);
			}
			this._hub.gameConsoleTransmission.SendToClient("Hi " + this._hub.nicknameSync.MyNick + "! You have been authenticated on this server.", "green");
			this._hub.serverRoles.RefreshPermissions(false);
			if (PlayerAuthenticationManager.AllowSameAccountJoining)
			{
				return;
			}
			int playerId = ReferenceHub.GetHub(base.gameObject).PlayerId;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.authManager.UserId == this.UserId && referenceHub.PlayerId != playerId && !referenceHub.isLocalPlayer)
				{
					ServerConsole.AddLog(string.Format("Player {0} ({1}, {2}) has been kicked from the server, because he has just joined the server again from IP address {3}.", new object[]
					{
						this.UserId,
						referenceHub.PlayerId,
						base.connectionToClient.address,
						base.connectionToClient.address
					}), ConsoleColor.Gray, false);
					ServerConsole.Disconnect(referenceHub.gameObject, "Only one player instance of the same player is allowed.");
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
				ServerConsole.AddLog("Player kicked due to local UserID ban.", ConsoleColor.Gray, false);
				ServerConsole.Disconnect(this._hub.connectionToClient, "You are banned from this server.");
				return false;
			}
			if (CustomNetworkManager.IsVerified || CustomLiteNetLib4MirrorTransport.UseGlobalBans)
			{
				string globalBan = token.GlobalBan;
				if (!(globalBan == "NO"))
				{
					if (!(globalBan == "M1"))
					{
						if (!(globalBan == "M2"))
						{
							this._hub.gameConsoleTransmission.SendToClient(token.GlobalBan, "red");
							ServerConsole.AddLog("Player " + token.UserId + " has been kicked due to an active global ban: " + token.GlobalBan, ConsoleColor.Gray, false);
							ServerConsole.Disconnect(this._hub.connectionToClient, token.GlobalBan);
							return false;
						}
						ServerConsole.AddLog("Player " + token.UserId + " is globally muted on intercom.", ConsoleColor.Gray, false);
					}
					else
					{
						ServerConsole.AddLog("Player " + token.UserId + " is globally muted.", ConsoleColor.Gray, false);
					}
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
							string[] array = address.Split('.', StringSplitOptions.None);
							byte b;
							if (array.Length == 4 && byte.TryParse(array[1], out b) && b >= 16 && b <= 31)
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
						ServerConsole.AddLog("Player kicked due to IP addresses mismatch.", ConsoleColor.Gray, false);
						ServerConsole.Disconnect(this._hub.connectionToClient, "Authentication token has been issued to a different IP address. You can find details in the game console.");
						return false;
					}
				}
			}
			VcMuteFlags vcMuteFlags = VcMuteFlags.None;
			if (VoiceChatMutes.QueryLocalMute(unsalted, false))
			{
				vcMuteFlags |= VcMuteFlags.LocalRegular;
				this._hub.gameConsoleTransmission.SendToClient("You are muted on the voice chat by the server administrator.", "red");
			}
			if ((ConfigFile.ServerConfig.GetBool("global_mutes_voicechat", true) || CustomNetworkManager.IsVerified) && token.GlobalBan == "M1")
			{
				vcMuteFlags |= VcMuteFlags.GlobalRegular;
				this._hub.gameConsoleTransmission.SendToClient("You are globally muted on the voice chat.", "red");
			}
			if (VoiceChatMutes.QueryLocalMute(unsalted, true))
			{
				vcMuteFlags |= VcMuteFlags.LocalIntercom;
				this._hub.gameConsoleTransmission.SendToClient("You are muted on the intercom by the server administrator.", "red");
			}
			else if ((ConfigFile.ServerConfig.GetBool("global_mutes_intercom", true) || CustomNetworkManager.IsVerified) && token.GlobalBan == "M2")
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
			if (userId == null)
			{
				return null;
			}
			if (userId.Contains("$"))
			{
				return userId.Substring(0, userId.IndexOf("$", StringComparison.Ordinal));
			}
			return userId;
		}

		[Command]
		private void CmdHandlePasswordAuthentication(string clientSalt, byte[] signature, string ecdhPublicKey)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(clientSalt);
			networkWriterPooled.WriteBytesAndSize(signature);
			networkWriterPooled.WriteString(ecdhPublicKey);
			base.SendCommandInternal("System.Void CentralAuth.PlayerAuthenticationManager::CmdHandlePasswordAuthentication(System.String,System.Byte[],System.String)", 1988415082, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[TargetRpc]
		private void RpcFinishExchange(string publicKey, byte[] signature)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteString(publicKey);
			networkWriterPooled.WriteBytesAndSize(signature);
			this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcFinishExchange(System.String,System.Byte[])", 112993832, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[TargetRpc]
		private void RpcAnimateInvalidPassword()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendTargetRPCInternal(null, "System.Void CentralAuth.PlayerAuthenticationManager::RpcAnimateInvalidPassword()", 2027815992, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Server]
		public void ResetPasswordAttempts()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void CentralAuth.PlayerAuthenticationManager::ResetPasswordAttempts()' called when server was not active");
				return;
			}
			this._passwordAttempts = 0U;
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
				ServerConsole.AddLog(text, ConsoleColor.DarkYellow, false);
				ServerLogs.AddLog(ServerLogs.Modules.Permissions, text, ServerLogs.ServerLogType.ConnectionUpdate, false);
				this._hub.serverRoles.SetGroup(overrideGroup, true, false);
				return;
			}
			ServerConsole.AddLog("Non-existing group is assigned for override password!", ConsoleColor.Red, false);
			this._hub.gameConsoleTransmission.SendToClient("Non-existing group is assigned for override password!", "red");
		}

		private static byte[] DerivePassword(string password, string serversalt, string clientsalt)
		{
			byte[] array = Sha.Sha512(serversalt + "/" + clientsalt);
			return PBKDF2.Pbkdf2HashBytes(password, array, 1600, 512);
		}

		private void UserIdHook(string p, string i)
		{
			Action<ReferenceHub> onSyncedUserIdAssigned = PlayerAuthenticationManager.OnSyncedUserIdAssigned;
			if (onSyncedUserIdAssigned != null)
			{
				onSyncedUserIdAssigned(this._hub);
			}
			if (string.IsNullOrEmpty(i))
			{
				this.InstanceMode = ClientInstanceMode.Unverified;
				return;
			}
			ClientInstanceMode clientInstanceMode;
			if (!(i == "ID_Dedicated"))
			{
				if (!(i == "ID_Host"))
				{
					if (!(i == "ID_Dummy"))
					{
						clientInstanceMode = ClientInstanceMode.ReadyClient;
					}
					else
					{
						clientInstanceMode = ClientInstanceMode.Dummy;
					}
				}
				else
				{
					clientInstanceMode = ClientInstanceMode.Host;
				}
			}
			else
			{
				clientInstanceMode = ClientInstanceMode.DedicatedServer;
			}
			this.InstanceMode = clientInstanceMode;
		}

		static PlayerAuthenticationManager()
		{
			RemoteProcedureCalls.RegisterCommand(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::CmdHandlePasswordAuthentication(System.String,System.Byte[],System.String)", new RemoteCallDelegate(PlayerAuthenticationManager.InvokeUserCode_CmdHandlePasswordAuthentication__String__Byte[]__String), true);
			RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcRequestAuthentication(System.String,System.String)", new RemoteCallDelegate(PlayerAuthenticationManager.InvokeUserCode_RpcRequestAuthentication__String__String));
			RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::TargetSetRealId(Mirror.NetworkConnection,System.String)", new RemoteCallDelegate(PlayerAuthenticationManager.InvokeUserCode_TargetSetRealId__NetworkConnection__String));
			RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcFinishExchange(System.String,System.Byte[])", new RemoteCallDelegate(PlayerAuthenticationManager.InvokeUserCode_RpcFinishExchange__String__Byte[]));
			RemoteProcedureCalls.RegisterRpc(typeof(PlayerAuthenticationManager), "System.Void CentralAuth.PlayerAuthenticationManager::RpcAnimateInvalidPassword()", new RemoteCallDelegate(PlayerAuthenticationManager.InvokeUserCode_RpcAnimateInvalidPassword));
		}

		public override bool Weaved()
		{
			return true;
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
				base.GeneratedSyncVarSetter<string>(value, ref this.SyncedUserId, 1UL, new Action<string, string>(this.UserIdHook));
			}
		}

		protected void UserCode_RpcRequestAuthentication__String__String(string challenge, string ecdhPublicKey)
		{
		}

		protected static void InvokeUserCode_RpcRequestAuthentication__String__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC RpcRequestAuthentication called on server.");
				return;
			}
			((PlayerAuthenticationManager)obj).UserCode_RpcRequestAuthentication__String__String(reader.ReadString(), reader.ReadString());
		}

		protected void UserCode_TargetSetRealId__NetworkConnection__String(NetworkConnection conn, string userId)
		{
		}

		protected static void InvokeUserCode_TargetSetRealId__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC TargetSetRealId called on server.");
				return;
			}
			((PlayerAuthenticationManager)obj).UserCode_TargetSetRealId__NetworkConnection__String(null, reader.ReadString());
		}

		protected void UserCode_CmdHandlePasswordAuthentication__String__Byte[]__String(string clientSalt, byte[] signature, string ecdhPublicKey)
		{
			if (this._passwordAttempts > 2U)
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
			byte[] array = PlayerAuthenticationManager.DerivePassword(ServerStatic.PermissionsHandler.OverridePassword, hostHub.encryptedChannelManager.ServerRandom, clientSalt);
			if (PlayerAuthenticationManager.OnlineMode)
			{
				if (this.SyncedUserId == null)
				{
					this._hub.gameConsoleTransmission.SendToClient("Can't process password auth request - not authenticated while server is in online mode.", "red");
					return;
				}
				if (!Sha.Sha512Hmac(array, this.SyncedUserId).SequenceEqual(signature))
				{
					this._passwordAttempts += 1U;
					string text = this._hub.LoggedNameFromRefHub() + " attempted to use an invalid RemoteAdmin override password.";
					ServerConsole.AddLog(text, ConsoleColor.Magenta, false);
					ServerLogs.AddLog(ServerLogs.Modules.Permissions, text, ServerLogs.ServerLogType.ConnectionUpdate, false);
					this.RpcAnimateInvalidPassword();
					return;
				}
				this.AssignPasswordOverrideGroup();
				return;
			}
			else
			{
				if (ecdhPublicKey == null)
				{
					this._hub.gameConsoleTransmission.SendToClient("Can't process password auth request - ecdhPublicKey is null in offline mode.", "red");
					return;
				}
				if (!Sha.Sha512Hmac(array, ecdhPublicKey).SequenceEqual(signature))
				{
					this._passwordAttempts += 1U;
					string text2 = this._hub.LoggedNameFromRefHub() + " attempted to use an invalid RemoteAdmin override password.";
					ServerConsole.AddLog(text2, ConsoleColor.Magenta, false);
					ServerLogs.AddLog(ServerLogs.Modules.Permissions, text2, ServerLogs.ServerLogType.ConnectionUpdate, false);
					this.RpcAnimateInvalidPassword();
					return;
				}
				this._hub.encryptedChannelManager.PrepareExchange();
				this._hub.encryptedChannelManager.ServerProcessExchange(ecdhPublicKey);
				string text3 = ECDSA.KeyToString(this._hub.encryptedChannelManager.EcdhKeys.Public);
				byte[] array2 = Sha.Sha512Hmac(array, text3);
				this.RpcFinishExchange(text3, array2);
				this.AssignPasswordOverrideGroup();
				return;
			}
		}

		protected static void InvokeUserCode_CmdHandlePasswordAuthentication__String__Byte[]__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				Debug.LogError("Command CmdHandlePasswordAuthentication called on client.");
				return;
			}
			((PlayerAuthenticationManager)obj).UserCode_CmdHandlePasswordAuthentication__String__Byte[]__String(reader.ReadString(), reader.ReadBytesAndSize(), reader.ReadString());
		}

		protected void UserCode_RpcFinishExchange__String__Byte[](string publicKey, byte[] signature)
		{
		}

		protected static void InvokeUserCode_RpcFinishExchange__String__Byte[](NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC RpcFinishExchange called on server.");
				return;
			}
			((PlayerAuthenticationManager)obj).UserCode_RpcFinishExchange__String__Byte[](reader.ReadString(), reader.ReadBytesAndSize());
		}

		protected void UserCode_RpcAnimateInvalidPassword()
		{
		}

		protected static void InvokeUserCode_RpcAnimateInvalidPassword(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC RpcAnimateInvalidPassword called on server.");
				return;
			}
			((PlayerAuthenticationManager)obj).UserCode_RpcAnimateInvalidPassword();
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
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteString(this.SyncedUserId);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<string>(ref this.SyncedUserId, new Action<string, string>(this.UserIdHook), reader.ReadString());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<string>(ref this.SyncedUserId, new Action<string, string>(this.UserIdHook), reader.ReadString());
			}
		}

		[SyncVar(hook = "UserIdHook")]
		public string SyncedUserId;

		public static bool OnlineMode;

		internal static bool AllowSameAccountJoining;

		public static uint AuthenticationTimeout;

		private static readonly Regex _saltRegex = new Regex("^[a-zA-Z0-9]{32}$", RegexOptions.Compiled);

		private bool _hubSet;

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
	}
}
