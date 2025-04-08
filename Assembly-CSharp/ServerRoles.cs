using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CentralAuth;
using Cryptography;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

public class ServerRoles : NetworkBehaviour
{
	public bool BypassMode
	{
		get
		{
			return this._bypassMode;
		}
		set
		{
			this._bypassMode = value;
			this._hub.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.BypassMode, value);
		}
	}

	public bool IsInOverwatch
	{
		get
		{
			return this._hub.roleManager.CurrentRole is OverwatchRole;
		}
		set
		{
			if (value == this.IsInOverwatch)
			{
				return;
			}
			this._hub.roleManager.ServerSetRole(value ? RoleTypeId.Overwatch : RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
		}
	}

	public UserGroup Group
	{
		get
		{
			return this._group;
		}
		set
		{
			this.SetGroup(value, false, false);
		}
	}

	private Dictionary<string, ServerRoles.NamedColor> NamedColorsDic
	{
		get
		{
			if (ServerRoles._colorDictionaryCacheSet)
			{
				return ServerRoles.DictionarizedColorsCache;
			}
			foreach (ServerRoles.NamedColor namedColor in this.NamedColors)
			{
				ServerRoles.DictionarizedColorsCache[namedColor.Name] = namedColor;
			}
			ServerRoles._colorDictionaryCacheSet = true;
			return ServerRoles.DictionarizedColorsCache;
		}
	}

	internal byte KickPower
	{
		get
		{
			if (this._hub.authManager.RemoteAdminGlobalAccess)
			{
				return byte.MaxValue;
			}
			UserGroup group = this.Group;
			if (group == null)
			{
				return 0;
			}
			return group.KickPower;
		}
	}

	public string MyColor { get; private set; }

	public string MyText { get; private set; }

	public bool GlobalSet
	{
		get
		{
			return this.GlobalBadge != null || (this.MyText != null && this.MyText.StartsWith("[", StringComparison.Ordinal) && this.MyText.EndsWith("]", StringComparison.Ordinal));
		}
	}

	public static ServerRoles.BadgeVisibilityPreferences GetGlobalBadgePreferences()
	{
		return ServerRoles.BadgeVisibilityPreferences.NoPreference;
	}

	public void Start()
	{
		this._hub = ReferenceHub.GetHub(base.gameObject);
		if (this._hub.IsDummy)
		{
			this.SetGroup(DummyUtils.DummyGroup, true, false);
		}
	}

	[TargetRpc]
	private void TargetSetHiddenRole(NetworkConnection connection, string role)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(role);
		this.SendTargetRPCInternal(connection, "System.Void ServerRoles::TargetSetHiddenRole(Mirror.NetworkConnection,System.String)", -1356032325, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	public void RpcResetFixed()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void ServerRoles::RpcResetFixed()", 87745685, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[Command(channel = 4)]
	private void CmdSetLocalTagPreferences(ServerRoles.BadgePreferences userPreferences, ServerRoles.BadgeVisibilityPreferences globalPreferences, ServerRoles.BadgeVisibilityPreferences localPreferences, bool refresh)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		global::Mirror.GeneratedNetworkCode._Write_ServerRoles/BadgePreferences(networkWriterPooled, userPreferences);
		global::Mirror.GeneratedNetworkCode._Write_ServerRoles/BadgeVisibilityPreferences(networkWriterPooled, globalPreferences);
		global::Mirror.GeneratedNetworkCode._Write_ServerRoles/BadgeVisibilityPreferences(networkWriterPooled, localPreferences);
		networkWriterPooled.WriteBool(refresh);
		base.SendCommandInternal("System.Void ServerRoles::CmdSetLocalTagPreferences(ServerRoles/BadgePreferences,ServerRoles/BadgeVisibilityPreferences,ServerRoles/BadgeVisibilityPreferences,System.Boolean)", -1971566213, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[Server]
	private void RefreshGlobalBadgeVisibility(ServerRoles.BadgeVisibilityPreferences globalPreferences)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshGlobalBadgeVisibility(ServerRoles/BadgeVisibilityPreferences)' called when server was not active");
			return;
		}
		this.RefreshGlobalTag();
		if (globalPreferences != ServerRoles.BadgeVisibilityPreferences.Hidden)
		{
			return;
		}
		this.TryHideTag();
	}

	[Server]
	private void RefreshLocalBadgeVisibility(ServerRoles.BadgeVisibilityPreferences localPreferences)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshLocalBadgeVisibility(ServerRoles/BadgeVisibilityPreferences)' called when server was not active");
			return;
		}
		this.RefreshLocalTag();
		if (localPreferences != ServerRoles.BadgeVisibilityPreferences.Hidden)
		{
			return;
		}
		this.TryHideTag();
	}

	[Server]
	public void RefreshPermissions(bool disp = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshPermissions(System.Boolean)' called when server was not active");
			return;
		}
		UserGroup userGroup = ServerStatic.PermissionsHandler.GetUserGroup(this._hub.authManager.UserId);
		if (userGroup != null)
		{
			this.SetGroup(userGroup, false, disp);
		}
	}

	[Server]
	public void SetGroup(UserGroup group, bool byAdmin = false, bool disp = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::SetGroup(UserGroup,System.Boolean,System.Boolean)' called when server was not active");
			return;
		}
		PlayerGroupChangingEventArgs playerGroupChangingEventArgs = new PlayerGroupChangingEventArgs(this._hub, group);
		PlayerEvents.OnGroupChanging(playerGroupChangingEventArgs);
		if (!playerGroupChangingEventArgs.IsAllowed)
		{
			return;
		}
		group = playerGroupChangingEventArgs.Group;
		if (group == null)
		{
			this.RemoteAdmin = this.GlobalPerms > 0UL;
			this.Permissions = this.GlobalPerms;
			this.GlobalHidden = false;
			this._group = null;
			this.SetColor(null);
			this.SetText(null);
			this.BadgeCover = false;
			this.RpcResetFixed();
			this.FinalizeSetGroup();
			this._hub.gameConsoleTransmission.SendToClient("Your local permissions has been revoked by server administrator.", "red");
			return;
		}
		this._hub.gameConsoleTransmission.SendToClient((!byAdmin) ? "Updating your group on server (local permissions)..." : "Updating your group on server (set by server administrator)...", "cyan");
		this._group = group;
		this.BadgeCover = group.Cover;
		if ((group.Permissions | this.GlobalPerms) > 0UL && ServerStatic.PermissionsHandler.IsRaPermitted(group.Permissions | this.GlobalPerms))
		{
			this.Permissions = group.Permissions | this.GlobalPerms;
			this._hub.authManager.ResetPasswordAttempts();
			this._hub.gameConsoleTransmission.SendToClient((!byAdmin) ? "Your remote admin access has been granted (local permissions)." : "Your remote admin access has been granted (set by server administrator).", "cyan");
		}
		else
		{
			this.Permissions = group.Permissions | this.GlobalPerms;
		}
		ServerLogs.AddLog(ServerLogs.Modules.Permissions, this._hub.LoggedNameFromRefHub() + " has been assigned to group " + group.BadgeText + ".", ServerLogs.ServerLogType.ConnectionUpdate, false);
		if (group.BadgeColor == "none")
		{
			this.RpcResetFixed();
			this.FinalizeSetGroup();
			return;
		}
		if ((this._localBadgeVisibilityPreferences == ServerRoles.BadgeVisibilityPreferences.Hidden && !disp) || (group.HiddenByDefault && !disp && this._localBadgeVisibilityPreferences != ServerRoles.BadgeVisibilityPreferences.Visible))
		{
			this.BadgeCover = this.UserBadgePreferences == ServerRoles.BadgePreferences.PreferLocal;
			if (!string.IsNullOrEmpty(this.MyText))
			{
				return;
			}
			this.SetText(null);
			this.SetColor("default");
			this.GlobalHidden = false;
			this.HiddenBadge = group.BadgeText;
			this.RefreshHiddenTag();
			this.TargetSetHiddenRole(base.connectionToClient, group.BadgeText);
			if (!byAdmin)
			{
				this._hub.gameConsoleTransmission.SendToClient("Your role has been granted, but it's hidden. Use \"showtag\" command in the game console to show your server badge.", "yellow");
			}
			else
			{
				this._hub.gameConsoleTransmission.SendToClient("Your role has been granted to you (set by server administrator), but it's hidden. Use \"showtag\" command in the game console to show your server badge.", "cyan");
			}
		}
		else
		{
			this.HiddenBadge = null;
			this.GlobalHidden = false;
			this.RpcResetFixed();
			this.SetText(group.BadgeText);
			this.SetColor(group.BadgeColor);
			if (!byAdmin)
			{
				this._hub.gameConsoleTransmission.SendToClient(string.Concat(new string[] { "Your role \"", group.BadgeText, "\" with color ", group.BadgeColor, " has been granted to you (local permissions)." }), "cyan");
			}
			else
			{
				this._hub.gameConsoleTransmission.SendToClient(string.Concat(new string[] { "Your role \"", group.BadgeText, "\" with color ", group.BadgeColor, " has been granted to you (set by server administrator)." }), "cyan");
			}
		}
		this.FinalizeSetGroup();
	}

	[Server]
	public void FinalizeSetGroup()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::FinalizeSetGroup()' called when server was not active");
			return;
		}
		this.Permissions |= this.GlobalPerms;
		this.RemoteAdmin = ServerStatic.PermissionsHandler.IsRaPermitted(this.Permissions | this.GlobalPerms);
		this.AdminChatPerms = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.AdminChat);
		this._hub.queryProcessor.GameplayData = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.GameplayData);
		if (this.RemoteAdmin)
		{
			this.OpenRemoteAdmin();
		}
		else
		{
			this.TargetSetRemoteAdmin(false);
		}
		this.SendRealIds();
		bool flag = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.ViewHiddenBadges);
		bool flag2 = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.ViewHiddenGlobalBadges);
		if (flag || flag2)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.Mode != ClientInstanceMode.DedicatedServer)
				{
					ServerRoles serverRoles = referenceHub.serverRoles;
					if (!string.IsNullOrEmpty(serverRoles.HiddenBadge) && (!serverRoles.GlobalHidden || flag2) && (serverRoles.GlobalHidden || flag))
					{
						serverRoles.TargetSetHiddenRole(base.connectionToClient, serverRoles.HiddenBadge);
					}
				}
			}
			if (flag && flag2)
			{
				this._hub.gameConsoleTransmission.SendToClient("Hidden badges (local and global) have been displayed for you (if there are any).", "gray");
			}
			else if (flag)
			{
				this._hub.gameConsoleTransmission.SendToClient("Hidden badges (local only) have been displayed for you (if there are any).", "gray");
			}
			else
			{
				this._hub.gameConsoleTransmission.SendToClient("Hidden badges (global only) have been displayed for you (if there are any).", "gray");
			}
		}
		PlayerEvents.OnGroupChanged(new PlayerGroupChangedEventArgs(this._hub, this._group));
	}

	[ServerCallback]
	public void RefreshHiddenTag()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (referenceHub.Mode != ClientInstanceMode.DedicatedServer)
			{
				ServerRoles serverRoles = referenceHub.serverRoles;
				bool flag = PermissionsHandler.IsPermitted(serverRoles.Permissions, PlayerPermissions.ViewHiddenBadges);
				bool flag2 = PermissionsHandler.IsPermitted(serverRoles.Permissions, PlayerPermissions.ViewHiddenGlobalBadges);
				if ((!this.GlobalHidden || flag2) && (this.GlobalHidden || flag))
				{
					this.TargetSetHiddenRole(serverRoles.connectionToClient, this.HiddenBadge);
				}
			}
		}
	}

	[Server]
	public void RefreshRealId()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshRealId()' called when server was not active");
			return;
		}
		if (this._hub == null)
		{
			return;
		}
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (referenceHub.Mode != ClientInstanceMode.DedicatedServer && (PermissionsHandler.IsPermitted(referenceHub.serverRoles.Permissions, 18007046UL) || referenceHub.authManager.NorthwoodStaff))
			{
				this._hub.authManager.TargetSetRealId(referenceHub.networkIdentity.connectionToClient, this._hub.authManager.UserId);
			}
		}
	}

	[Server]
	private void SendRealIds()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::SendRealIds()' called when server was not active");
			return;
		}
		if (this._hub.Mode == ClientInstanceMode.DedicatedServer)
		{
			return;
		}
		bool flag = this._hub.authManager.NorthwoodStaff || PermissionsHandler.IsPermitted(this.Permissions, 18007046UL);
		if (!flag && !this._lastRealIdPerm)
		{
			return;
		}
		this._lastRealIdPerm = flag;
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			referenceHub.authManager.TargetSetRealId(this._hub.networkIdentity.connectionToClient, flag ? referenceHub.authManager.UserId : null);
		}
	}

	public string GetColoredRoleString(bool newLine = false)
	{
		if (string.IsNullOrEmpty(this.MyColor) || string.IsNullOrEmpty(this.MyText) || this.CurrentColor == null)
		{
			return string.Empty;
		}
		if ((this.CurrentColor.Restricted || this.HasNotAllowedText) && !this._authorizeBadge)
		{
			return string.Empty;
		}
		foreach (ServerRoles.NamedColor namedColor in this.NamedColors)
		{
			if (namedColor.Name == this.MyColor)
			{
				return string.Concat(new string[]
				{
					newLine ? "\n" : string.Empty,
					"<color=#",
					namedColor.ColorHex,
					">",
					this.MyText,
					"</color>"
				});
			}
		}
		return string.Empty;
	}

	public string GetUncoloredRoleString()
	{
		if (string.IsNullOrEmpty(this.MyText) && !string.IsNullOrEmpty(this.FixedBadge))
		{
			return this.FixedBadge;
		}
		if (string.IsNullOrEmpty(this.MyColor) || string.IsNullOrEmpty(this.MyText) || this.CurrentColor == null)
		{
			return string.Empty;
		}
		if ((this.CurrentColor.Restricted || this.HasNotAllowedText) && !this._authorizeBadge)
		{
			return string.Empty;
		}
		return this.MyText;
	}

	public Color GetColor()
	{
		return Color.clear;
	}

	public Color GetVoiceColor()
	{
		ServerRoles.NamedColor namedColor;
		if (string.IsNullOrEmpty(this.MyColor) || !this.NamedColorsDic.TryGetValue(this.MyColor, out namedColor))
		{
			return this.NamedColorsDic["default"].SpeakingColor;
		}
		return namedColor.SpeakingColor;
	}

	private bool HasNotAllowedText
	{
		get
		{
			return this.MyText.Contains("[", StringComparison.Ordinal) || this.MyText.Contains("]", StringComparison.Ordinal) || this.MyText.Contains("<", StringComparison.Ordinal) || this.MyText.Contains(">", StringComparison.Ordinal) || this.MyText.Contains("\\u003c", StringComparison.Ordinal) || this.MyText.Contains("\\u003e", StringComparison.Ordinal);
		}
	}

	private void Update()
	{
		if (!string.IsNullOrEmpty(this.FixedBadge) && this.MyText != this.FixedBadge)
		{
			this.SetText(this.FixedBadge);
			this.SetColor("silver");
			return;
		}
		ServerRoles.NamedColor namedColor;
		if (!string.IsNullOrEmpty(this.FixedBadge))
		{
			namedColor = this.CurrentColor;
			if (namedColor == null || !(namedColor.Name == "silver"))
			{
				this.SetColor("silver");
				return;
			}
		}
		if (this.GlobalBadge != this._prevBadge)
		{
			this._prevBadge = this.GlobalBadge;
			if (string.IsNullOrEmpty(this.GlobalBadge))
			{
				this._bgc = null;
				this._bgt = null;
				this._authorizeBadge = false;
				if (this._prevColor != null)
				{
					this._prevColor += ".";
				}
				else
				{
					this._prevColor = ".";
				}
				if (this._prevText != null)
				{
					this._prevText += ".";
					return;
				}
				this._prevText = ".";
				return;
			}
			else
			{
				global::GameCore.Console.AddDebugLog("SDAUTH", "Validating global badge of user " + this._hub.nicknameSync.MyNick, MessageImportance.LessImportant, false);
				BadgeToken badgeToken;
				string text;
				string text2;
				if (new SignedToken(this.GlobalBadge, this.GlobalBadgeSignature).TryGetToken<BadgeToken>("Badge request", out badgeToken, out text, out text2, 0))
				{
					if (badgeToken.UserId != this._hub.authManager.SaltedUserId && badgeToken.UserId != Sha.HashToString(Sha.Sha512(this._hub.authManager.SaltedUserId)))
					{
						text = "badge token UserID mismatch.";
					}
					else if (StringUtils.Base64Decode(badgeToken.Nickname) != this._hub.nicknameSync.MyNick)
					{
						text = "badge token nickname mismatch.";
					}
					if (text != null)
					{
						global::GameCore.Console.AddDebugLog("SDAUTH", string.Concat(new string[]
						{
							"<color=red>Validation of global badge of user ",
							this._hub.nicknameSync.MyNick,
							" failed - ",
							text,
							".</color>"
						}), MessageImportance.Normal, false);
						this._bgc = null;
						this._bgt = null;
						this._authorizeBadge = false;
						if (this._prevColor != null)
						{
							this._prevColor += ".";
						}
						else
						{
							this._prevColor = ".";
						}
						if (this._prevText != null)
						{
							this._prevText += ".";
							return;
						}
						this._prevText = ".";
						return;
					}
					else
					{
						global::GameCore.Console.AddDebugLog("SDAUTH", string.Concat(new string[]
						{
							"Validation of global badge of user ",
							base.GetComponent<NicknameSync>().MyNick,
							" complete - badge signed by central server ",
							badgeToken.IssuedBy,
							"."
						}), MessageImportance.LessImportant, false);
						text2 = badgeToken.BadgeText;
						if (text2 == null || text2 == "(none)")
						{
							text2 = badgeToken.BadgeColor;
							if (text2 == null || text2 == "(none)")
							{
								this._bgc = null;
								this._bgt = null;
								this._authorizeBadge = false;
								goto IL_03FA;
							}
						}
						this._bgc = badgeToken.BadgeColor;
						this.SetColor(this._bgc);
						this._bgt = badgeToken.BadgeText;
						this.MyText = this._bgt;
						this._authorizeBadge = true;
					}
				}
				else
				{
					global::GameCore.Console.AddDebugLog("SDAUTH", string.Concat(new string[]
					{
						"<color=red>Validation of global badge of user ",
						this._hub.nicknameSync.MyNick,
						" failed - ",
						text,
						".</color>"
					}), MessageImportance.Normal, false);
					this._bgc = null;
					this._bgt = null;
					this._authorizeBadge = false;
					if (this._prevColor != null)
					{
						this._prevColor += ".";
					}
					else
					{
						this._prevColor = ".";
					}
					if (this._prevText != null)
					{
						this._prevText += ".";
						return;
					}
					this._prevText = ".";
					return;
				}
			}
		}
		IL_03FA:
		if (this._prevColor == this.MyColor && this._prevText == this.MyText)
		{
			return;
		}
		namedColor = this.CurrentColor;
		if (namedColor != null && namedColor.Restricted && (this.MyText != this._bgt || this.MyColor != this._bgc))
		{
			global::GameCore.Console.AddLog(string.Concat(new string[] { "TAG FAIL 1 - ", this.MyText, " - ", this._bgt, " /-/ ", this.MyColor, " - ", this._bgc }), Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
			this._authorizeBadge = false;
			this.SetColor(null);
			this._prevColor = null;
			PlayerList.UpdatePlayerRole(this._hub);
			return;
		}
		if (this.MyText != null && this.MyText != this._bgt && this.HasNotAllowedText)
		{
			global::GameCore.Console.AddLog(string.Concat(new string[] { "TAG FAIL 2 - ", this.MyText, " - ", this._bgt, " /-/ ", this.MyColor, " - ", this._bgc }), Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
			this._authorizeBadge = false;
			this.SetText(null);
			this._prevText = null;
			PlayerList.UpdatePlayerRole(this._hub);
			return;
		}
		this._prevColor = this.MyColor;
		this._prevText = this.MyText;
		this._prevBadge = this.GlobalBadge;
		PlayerList.UpdatePlayerRole(this._hub);
	}

	private void SetColorHook(string p, string i)
	{
		this.SetColor(i);
	}

	public void SetColor(string i)
	{
		if (string.IsNullOrEmpty(i))
		{
			i = "default";
		}
		if (NetworkServer.active)
		{
			this.Network_myColor = i;
		}
		this.MyColor = i;
		ServerRoles.NamedColor namedColor = this.NamedColors.FirstOrDefault((ServerRoles.NamedColor row) => row.Name == this.MyColor);
		if (namedColor == null && i != "default")
		{
			this.SetColor("default");
			return;
		}
		this.CurrentColor = namedColor;
	}

	private void SetTextHook(string p, string i)
	{
		this.SetText(i);
	}

	public void SetText(string i)
	{
		if (i == string.Empty)
		{
			i = null;
		}
		if (NetworkServer.active)
		{
			this.Network_myText = i;
		}
		this.MyText = i;
		ServerRoles.NamedColor namedColor = this.NamedColors.FirstOrDefault((ServerRoles.NamedColor row) => row.Name == this.MyColor);
		if (namedColor == null)
		{
			return;
		}
		this.CurrentColor = namedColor;
	}

	public bool HasBadgeHidden
	{
		get
		{
			return !this._hub.authManager.BypassBansFlagSet && !string.IsNullOrEmpty(this.HiddenBadge);
		}
	}

	public bool HasGlobalBadge
	{
		get
		{
			return this._hub.authManager.AuthenticationResponse.SignedBadgeToken != null;
		}
	}

	[Server]
	public bool RefreshGlobalTag()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean ServerRoles::RefreshGlobalTag()' called when server was not active");
			return default(bool);
		}
		if (!this.HasGlobalBadge)
		{
			return false;
		}
		this.SetColor(null);
		this.SetText(null);
		this.NetworkGlobalBadge = this._hub.authManager.AuthenticationResponse.SignedBadgeToken.token;
		this.NetworkGlobalBadgeSignature = this._hub.authManager.AuthenticationResponse.SignedBadgeToken.signature;
		this.HiddenBadge = null;
		this.GlobalHidden = false;
		this.RpcResetFixed();
		return true;
	}

	[Server]
	public void RefreshLocalTag()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshLocalTag()' called when server was not active");
			return;
		}
		this.NetworkGlobalBadge = null;
		this.NetworkGlobalBadgeSignature = null;
		this.HiddenBadge = null;
		this.GlobalHidden = false;
		this.RpcResetFixed();
		this.RefreshPermissions(true);
	}

	[Server]
	public bool TryHideTag()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean ServerRoles::TryHideTag()' called when server was not active");
			return default(bool);
		}
		if (string.IsNullOrEmpty(this.MyText))
		{
			if (this.GlobalBadge != null && this._hub.authManager.AuthenticationResponse.BadgeToken != null)
			{
				this.HiddenBadge = this._hub.authManager.AuthenticationResponse.BadgeToken.BadgeText;
				this.GlobalHidden = true;
			}
			else
			{
				if (!this._hub.authManager.BypassBansFlagSet)
				{
					return false;
				}
				this.GlobalHidden = false;
				this.HiddenBadge = null;
			}
		}
		else
		{
			this.GlobalHidden = this.GlobalSet;
			this.HiddenBadge = this.MyText;
		}
		this.NetworkGlobalBadge = null;
		this.SetText(null);
		this.SetColor(null);
		this.RefreshHiddenTag();
		return true;
	}

	internal void OpenRemoteAdmin()
	{
		this.TargetSetRemoteAdmin(true);
		this._hub.queryProcessor.SyncCommandsToClient();
	}

	[TargetRpc]
	private void TargetSetRemoteAdmin(bool open)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteBool(open);
		this.SendTargetRPCInternal(null, "System.Void ServerRoles::TargetSetRemoteAdmin(System.Boolean)", -586263322, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	static ServerRoles()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(ServerRoles), "System.Void ServerRoles::CmdSetLocalTagPreferences(ServerRoles/BadgePreferences,ServerRoles/BadgeVisibilityPreferences,ServerRoles/BadgeVisibilityPreferences,System.Boolean)", new RemoteCallDelegate(ServerRoles.InvokeUserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean), true);
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::RpcResetFixed()", new RemoteCallDelegate(ServerRoles.InvokeUserCode_RpcResetFixed));
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::TargetSetHiddenRole(Mirror.NetworkConnection,System.String)", new RemoteCallDelegate(ServerRoles.InvokeUserCode_TargetSetHiddenRole__NetworkConnection__String));
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::TargetSetRemoteAdmin(System.Boolean)", new RemoteCallDelegate(ServerRoles.InvokeUserCode_TargetSetRemoteAdmin__Boolean));
	}

	public override bool Weaved()
	{
		return true;
	}

	public string Network_myText
	{
		get
		{
			return this._myText;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this._myText, 1UL, new Action<string, string>(this.SetTextHook));
		}
	}

	public string Network_myColor
	{
		get
		{
			return this._myColor;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this._myColor, 2UL, new Action<string, string>(this.SetColorHook));
		}
	}

	public string NetworkGlobalBadge
	{
		get
		{
			return this.GlobalBadge;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.GlobalBadge, 4UL, null);
		}
	}

	public string NetworkGlobalBadgeSignature
	{
		get
		{
			return this.GlobalBadgeSignature;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.GlobalBadgeSignature, 8UL, null);
		}
	}

	protected void UserCode_TargetSetHiddenRole__NetworkConnection__String(NetworkConnection connection, string role)
	{
		if (base.isServer)
		{
			return;
		}
		if (string.IsNullOrEmpty(role))
		{
			this.SetColor(null);
			this.SetText(null);
			this.FixedBadge = null;
			this.SetText(null);
			return;
		}
		this.SetColor("silver");
		this.FixedBadge = Misc.SanitizeRichText(role.Replace("[", string.Empty).Replace("]", string.Empty), "", "") + " " + TranslationReader.Get("Legacy_Interfaces", 18, "(hidden)");
		this.SetText(this.FixedBadge);
	}

	protected static void InvokeUserCode_TargetSetHiddenRole__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetHiddenRole called on server.");
			return;
		}
		((ServerRoles)obj).UserCode_TargetSetHiddenRole__NetworkConnection__String(null, reader.ReadString());
	}

	protected void UserCode_RpcResetFixed()
	{
		this.FixedBadge = null;
	}

	protected static void InvokeUserCode_RpcResetFixed(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcResetFixed called on server.");
			return;
		}
		((ServerRoles)obj).UserCode_RpcResetFixed();
	}

	protected void UserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(ServerRoles.BadgePreferences userPreferences, ServerRoles.BadgeVisibilityPreferences globalPreferences, ServerRoles.BadgeVisibilityPreferences localPreferences, bool refresh)
	{
		this.UserBadgePreferences = userPreferences;
		this._globalBadgeVisibilityPreferences = globalPreferences;
		this._localBadgeVisibilityPreferences = localPreferences;
		if (!refresh)
		{
			return;
		}
		if (userPreferences == ServerRoles.BadgePreferences.PreferGlobal && this.HasGlobalBadge)
		{
			this.RefreshGlobalBadgeVisibility(globalPreferences);
			return;
		}
		if (this.Group == null)
		{
			if (this.HasGlobalBadge)
			{
				this.RefreshGlobalBadgeVisibility(globalPreferences);
			}
			return;
		}
		this.RefreshLocalBadgeVisibility(localPreferences);
	}

	protected static void InvokeUserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetLocalTagPreferences called on client.");
			return;
		}
		((ServerRoles)obj).UserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(global::Mirror.GeneratedNetworkCode._Read_ServerRoles/BadgePreferences(reader), global::Mirror.GeneratedNetworkCode._Read_ServerRoles/BadgeVisibilityPreferences(reader), global::Mirror.GeneratedNetworkCode._Read_ServerRoles/BadgeVisibilityPreferences(reader), reader.ReadBool());
	}

	protected void UserCode_TargetSetRemoteAdmin__Boolean(bool open)
	{
	}

	protected static void InvokeUserCode_TargetSetRemoteAdmin__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetRemoteAdmin called on server.");
			return;
		}
		((ServerRoles)obj).UserCode_TargetSetRemoteAdmin__Boolean(reader.ReadBool());
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteString(this._myText);
			writer.WriteString(this._myColor);
			writer.WriteString(this.GlobalBadge);
			writer.WriteString(this.GlobalBadgeSignature);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteString(this._myText);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteString(this._myColor);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteString(this.GlobalBadge);
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteString(this.GlobalBadgeSignature);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._myText, new Action<string, string>(this.SetTextHook), reader.ReadString());
			base.GeneratedSyncVarDeserialize<string>(ref this._myColor, new Action<string, string>(this.SetColorHook), reader.ReadString());
			base.GeneratedSyncVarDeserialize<string>(ref this.GlobalBadge, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize<string>(ref this.GlobalBadgeSignature, null, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._myText, new Action<string, string>(this.SetTextHook), reader.ReadString());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._myColor, new Action<string, string>(this.SetColorHook), reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.GlobalBadge, null, reader.ReadString());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.GlobalBadgeSignature, null, reader.ReadString());
		}
	}

	public ServerRoles.NamedColor CurrentColor;

	public ServerRoles.NamedColor[] NamedColors;

	[NonSerialized]
	private bool _bypassMode;

	private bool _authorizeBadge;

	private UserGroup _group;

	private ReferenceHub _hub;

	private static readonly Dictionary<string, ServerRoles.NamedColor> DictionarizedColorsCache = new Dictionary<string, ServerRoles.NamedColor>();

	private static bool _colorDictionaryCacheSet;

	private const string DefaultColor = "default";

	private const string HiddenBadgeColor = "silver";

	public const ulong UserIdPerms = 18007046UL;

	private string _globalBadgeUnconfirmed;

	private string _prevColor;

	private string _prevText;

	private string _prevBadge;

	private string _authChallenge;

	private string _badgeChallenge;

	private string _bgc;

	private string _bgt;

	private bool _requested;

	private bool _publicPartRequested;

	private bool _badgeRequested;

	private bool _authRequested;

	private bool _noclipReady;

	internal bool BadgeCover;

	[NonSerialized]
	public ServerRoles.BadgePreferences UserBadgePreferences;

	private ServerRoles.BadgeVisibilityPreferences _globalBadgeVisibilityPreferences;

	private ServerRoles.BadgeVisibilityPreferences _localBadgeVisibilityPreferences;

	[SyncVar(hook = "SetTextHook")]
	private string _myText;

	[SyncVar(hook = "SetColorHook")]
	private string _myColor;

	[SyncVar]
	public string GlobalBadge;

	[SyncVar]
	public string GlobalBadgeSignature;

	[NonSerialized]
	public bool RemoteAdmin;

	[NonSerialized]
	public ulong Permissions;

	[NonSerialized]
	public string HiddenBadge;

	[NonSerialized]
	public bool GlobalHidden;

	[NonSerialized]
	internal bool AdminChatPerms;

	[NonSerialized]
	internal ulong GlobalPerms;

	[NonSerialized]
	private bool _lastRealIdPerm;

	[NonSerialized]
	public string FixedBadge;

	[Serializable]
	public class NamedColor
	{
		public Color SpeakingColor
		{
			get
			{
				if (!this._speakingColorSet)
				{
					this._speakingColorSet = true;
					ColorUtility.TryParseHtmlString("#" + (string.IsNullOrEmpty(this._speakingOverride) ? this.ColorHex : this._speakingOverride), out this._speakingColorCache);
				}
				return this._speakingColorCache;
			}
		}

		public string Name;

		public string ColorHex;

		public bool Restricted;

		[SerializeField]
		private string _speakingOverride;

		private Color _speakingColorCache;

		private bool _speakingColorSet;
	}

	public enum BadgePreferences
	{
		NoPreference,
		PreferGlobal,
		PreferLocal
	}

	public enum BadgeVisibilityPreferences
	{
		NoPreference,
		Visible,
		Hidden
	}
}
