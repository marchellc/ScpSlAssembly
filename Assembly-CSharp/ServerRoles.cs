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
using NetworkManagerUtils.Dummies;
using NorthwoodLib;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

public class ServerRoles : NetworkBehaviour
{
	[Serializable]
	public class NamedColor
	{
		public string Name;

		public string ColorHex;

		public bool Restricted;

		[SerializeField]
		private string _speakingOverride;

		private Color _speakingColorCache;

		private bool _speakingColorSet;

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

	public NamedColor CurrentColor;

	public NamedColor[] NamedColors;

	[NonSerialized]
	private bool _bypassMode;

	private bool _authorizeBadge;

	private UserGroup _group;

	private ReferenceHub _hub;

	private static readonly Dictionary<string, NamedColor> DictionarizedColorsCache;

	private static bool _colorDictionaryCacheSet;

	private const string DefaultColor = "default";

	private const string HiddenBadgeColor = "silver";

	public const ulong UserIdPerms = 18007046uL;

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
	public BadgePreferences UserBadgePreferences;

	private BadgeVisibilityPreferences _globalBadgeVisibilityPreferences;

	private BadgeVisibilityPreferences _localBadgeVisibilityPreferences;

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
			if (value != this.IsInOverwatch)
			{
				this._hub.roleManager.ServerSetRole(value ? RoleTypeId.Overwatch : RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
			}
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
			this.SetGroup(value);
		}
	}

	private Dictionary<string, NamedColor> NamedColorsDic
	{
		get
		{
			if (ServerRoles._colorDictionaryCacheSet)
			{
				return ServerRoles.DictionarizedColorsCache;
			}
			NamedColor[] namedColors = this.NamedColors;
			foreach (NamedColor namedColor in namedColors)
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
			if (!this._hub.authManager.RemoteAdminGlobalAccess)
			{
				return this.Group?.KickPower ?? 0;
			}
			return byte.MaxValue;
		}
	}

	public string MyColor { get; private set; }

	public string MyText { get; private set; }

	public bool GlobalSet
	{
		get
		{
			if (this.GlobalBadge == null)
			{
				if (this.MyText != null && this.MyText.StartsWith("[", StringComparison.Ordinal))
				{
					return this.MyText.EndsWith("]", StringComparison.Ordinal);
				}
				return false;
			}
			return true;
		}
	}

	private bool HasNotAllowedText
	{
		get
		{
			if (!this.MyText.Contains("[", StringComparison.Ordinal) && !this.MyText.Contains("]", StringComparison.Ordinal) && !this.MyText.Contains("<", StringComparison.Ordinal) && !this.MyText.Contains(">", StringComparison.Ordinal) && !this.MyText.Contains("\\u003c", StringComparison.Ordinal))
			{
				return this.MyText.Contains("\\u003e", StringComparison.Ordinal);
			}
			return true;
		}
	}

	public bool HasBadgeHidden
	{
		get
		{
			if (!this._hub.authManager.BypassBansFlagSet)
			{
				return !string.IsNullOrEmpty(this.HiddenBadge);
			}
			return false;
		}
	}

	public bool HasGlobalBadge => this._hub.authManager.AuthenticationResponse.SignedBadgeToken != null;

	public string Network_myText
	{
		get
		{
			return this._myText;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._myText, 1uL, SetTextHook);
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
			base.GeneratedSyncVarSetter(value, ref this._myColor, 2uL, SetColorHook);
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
			base.GeneratedSyncVarSetter(value, ref this.GlobalBadge, 4uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this.GlobalBadgeSignature, 8uL, null);
		}
	}

	public static BadgeVisibilityPreferences GetGlobalBadgePreferences()
	{
		return BadgeVisibilityPreferences.NoPreference;
	}

	public void Start()
	{
		this._hub = ReferenceHub.GetHub(base.gameObject);
		if (this._hub.IsDummy)
		{
			this.SetGroup(DummyUtils.DummyGroup, byAdmin: true);
		}
	}

	[TargetRpc]
	private void TargetSetHiddenRole(NetworkConnection connection, string role)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(role);
		this.SendTargetRPCInternal(connection, "System.Void ServerRoles::TargetSetHiddenRole(Mirror.NetworkConnection,System.String)", -1356032325, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	public void RpcResetFixed()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void ServerRoles::RpcResetFixed()", 87745685, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[Command(channel = 4)]
	private void CmdSetLocalTagPreferences(BadgePreferences userPreferences, BadgeVisibilityPreferences globalPreferences, BadgeVisibilityPreferences localPreferences, bool refresh)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_ServerRoles_002FBadgePreferences(writer, userPreferences);
		GeneratedNetworkCode._Write_ServerRoles_002FBadgeVisibilityPreferences(writer, globalPreferences);
		GeneratedNetworkCode._Write_ServerRoles_002FBadgeVisibilityPreferences(writer, localPreferences);
		writer.WriteBool(refresh);
		base.SendCommandInternal("System.Void ServerRoles::CmdSetLocalTagPreferences(ServerRoles/BadgePreferences,ServerRoles/BadgeVisibilityPreferences,ServerRoles/BadgeVisibilityPreferences,System.Boolean)", -1971566213, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	private void RefreshGlobalBadgeVisibility(BadgeVisibilityPreferences globalPreferences)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshGlobalBadgeVisibility(ServerRoles/BadgeVisibilityPreferences)' called when server was not active");
			return;
		}
		this.RefreshGlobalTag();
		if (globalPreferences == BadgeVisibilityPreferences.Hidden)
		{
			this.TryHideTag();
		}
	}

	[Server]
	private void RefreshLocalBadgeVisibility(BadgeVisibilityPreferences localPreferences)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::RefreshLocalBadgeVisibility(ServerRoles/BadgeVisibilityPreferences)' called when server was not active");
			return;
		}
		this.RefreshLocalTag();
		if (localPreferences == BadgeVisibilityPreferences.Hidden)
		{
			this.TryHideTag();
		}
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
			this.SetGroup(userGroup, byAdmin: false, disp);
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
		PlayerGroupChangingEventArgs e = new PlayerGroupChangingEventArgs(this._hub, group);
		PlayerEvents.OnGroupChanging(e);
		if (!e.IsAllowed)
		{
			return;
		}
		group = e.Group;
		if (group == null)
		{
			this.RemoteAdmin = this.GlobalPerms != 0;
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
		if ((group.Permissions | this.GlobalPerms) != 0 && ServerStatic.PermissionsHandler.IsRaPermitted(group.Permissions | this.GlobalPerms))
		{
			this.Permissions = group.Permissions | this.GlobalPerms;
			this._hub.authManager.ResetPasswordAttempts();
			this._hub.gameConsoleTransmission.SendToClient((!byAdmin) ? "Your remote admin access has been granted (local permissions)." : "Your remote admin access has been granted (set by server administrator).", "cyan");
		}
		else
		{
			this.Permissions = group.Permissions | this.GlobalPerms;
		}
		ServerLogs.AddLog(ServerLogs.Modules.Permissions, this._hub.LoggedNameFromRefHub() + " has been assigned to group " + group.BadgeText + ".", ServerLogs.ServerLogType.ConnectionUpdate);
		if (group.BadgeColor == "none")
		{
			this.RpcResetFixed();
			this.FinalizeSetGroup();
			return;
		}
		if ((this._localBadgeVisibilityPreferences == BadgeVisibilityPreferences.Hidden && !disp) || (group.HiddenByDefault && !disp && this._localBadgeVisibilityPreferences != BadgeVisibilityPreferences.Visible))
		{
			this.BadgeCover = this.UserBadgePreferences == BadgePreferences.PreferLocal;
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
				this._hub.gameConsoleTransmission.SendToClient("Your role \"" + group.BadgeText + "\" with color " + group.BadgeColor + " has been granted to you (local permissions).", "cyan");
			}
			else
			{
				this._hub.gameConsoleTransmission.SendToClient("Your role \"" + group.BadgeText + "\" with color " + group.BadgeColor + " has been granted to you (set by server administrator).", "cyan");
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
			this.TargetSetRemoteAdmin(open: false);
		}
		this.SendRealIds();
		bool flag = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.ViewHiddenBadges);
		bool flag2 = PermissionsHandler.IsPermitted(this.Permissions, PlayerPermissions.ViewHiddenGlobalBadges);
		if (flag || flag2)
		{
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.Mode != ClientInstanceMode.DedicatedServer)
				{
					ServerRoles serverRoles = allHub.serverRoles;
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
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.Mode != ClientInstanceMode.DedicatedServer)
			{
				ServerRoles serverRoles = allHub.serverRoles;
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
		}
		else
		{
			if (this._hub == null)
			{
				return;
			}
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.Mode != ClientInstanceMode.DedicatedServer && (PermissionsHandler.IsPermitted(allHub.serverRoles.Permissions, 18007046uL) || allHub.authManager.NorthwoodStaff))
				{
					this._hub.authManager.TargetSetRealId(allHub.networkIdentity.connectionToClient, this._hub.authManager.UserId);
				}
			}
		}
	}

	[Server]
	private void SendRealIds()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerRoles::SendRealIds()' called when server was not active");
		}
		else
		{
			if (this._hub.Mode == ClientInstanceMode.DedicatedServer)
			{
				return;
			}
			bool flag = this._hub.authManager.NorthwoodStaff || PermissionsHandler.IsPermitted(this.Permissions, 18007046uL);
			if (!flag && !this._lastRealIdPerm)
			{
				return;
			}
			this._lastRealIdPerm = flag;
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				allHub.authManager.TargetSetRealId(this._hub.networkIdentity.connectionToClient, flag ? allHub.authManager.UserId : null);
			}
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
		NamedColor[] namedColors = this.NamedColors;
		foreach (NamedColor namedColor in namedColors)
		{
			if (namedColor.Name == this.MyColor)
			{
				return (newLine ? "\n" : string.Empty) + "<color=#" + namedColor.ColorHex + ">" + this.MyText + "</color>";
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
		if (string.IsNullOrEmpty(this.MyColor) || !this.NamedColorsDic.TryGetValue(this.MyColor, out var value))
		{
			return this.NamedColorsDic["default"].SpeakingColor;
		}
		return value.SpeakingColor;
	}

	private void Update()
	{
		if (!string.IsNullOrEmpty(this.FixedBadge) && this.MyText != this.FixedBadge)
		{
			this.SetText(this.FixedBadge);
			this.SetColor("silver");
			return;
		}
		if (!string.IsNullOrEmpty(this.FixedBadge))
		{
			NamedColor currentColor = this.CurrentColor;
			if (currentColor == null || !(currentColor.Name == "silver"))
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
				}
				else
				{
					this._prevText = ".";
				}
				return;
			}
			GameCore.Console.AddDebugLog("SDAUTH", "Validating global badge of user " + this._hub.nicknameSync.MyNick, MessageImportance.LessImportant);
			if (!new SignedToken(this.GlobalBadge, this.GlobalBadgeSignature).TryGetToken<BadgeToken>("Badge request", out var token, out var error, out var userId))
			{
				GameCore.Console.AddDebugLog("SDAUTH", "<color=red>Validation of global badge of user " + this._hub.nicknameSync.MyNick + " failed - " + error + ".</color>", MessageImportance.Normal);
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
				}
				else
				{
					this._prevText = ".";
				}
				return;
			}
			if (token.UserId != this._hub.authManager.SaltedUserId && token.UserId != Sha.HashToString(Sha.Sha512(this._hub.authManager.SaltedUserId)))
			{
				error = "badge token UserID mismatch.";
			}
			else if (StringUtils.Base64Decode(token.Nickname) != this._hub.nicknameSync.MyNick)
			{
				error = "badge token nickname mismatch.";
			}
			if (error != null)
			{
				GameCore.Console.AddDebugLog("SDAUTH", "<color=red>Validation of global badge of user " + this._hub.nicknameSync.MyNick + " failed - " + error + ".</color>", MessageImportance.Normal);
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
				}
				else
				{
					this._prevText = ".";
				}
				return;
			}
			GameCore.Console.AddDebugLog("SDAUTH", "Validation of global badge of user " + base.GetComponent<NicknameSync>().MyNick + " complete - badge signed by central server " + token.IssuedBy + ".", MessageImportance.LessImportant);
			userId = token.BadgeText;
			if (userId == null || userId == "(none)")
			{
				userId = token.BadgeColor;
				if (userId == null || userId == "(none)")
				{
					this._bgc = null;
					this._bgt = null;
					this._authorizeBadge = false;
					goto IL_03fa;
				}
			}
			this._bgc = token.BadgeColor;
			this.SetColor(this._bgc);
			this._bgt = token.BadgeText;
			this.MyText = this._bgt;
			this._authorizeBadge = true;
		}
		goto IL_03fa;
		IL_03fa:
		if (!(this._prevColor == this.MyColor) || !(this._prevText == this.MyText))
		{
			NamedColor currentColor = this.CurrentColor;
			if (currentColor != null && currentColor.Restricted && (this.MyText != this._bgt || this.MyColor != this._bgc))
			{
				GameCore.Console.AddLog("TAG FAIL 1 - " + this.MyText + " - " + this._bgt + " /-/ " + this.MyColor + " - " + this._bgc, Color.gray);
				this._authorizeBadge = false;
				this.SetColor(null);
				this._prevColor = null;
				PlayerList.UpdatePlayerRole(this._hub);
			}
			else if (this.MyText != null && this.MyText != this._bgt && this.HasNotAllowedText)
			{
				GameCore.Console.AddLog("TAG FAIL 2 - " + this.MyText + " - " + this._bgt + " /-/ " + this.MyColor + " - " + this._bgc, Color.gray);
				this._authorizeBadge = false;
				this.SetText(null);
				this._prevText = null;
				PlayerList.UpdatePlayerRole(this._hub);
			}
			else
			{
				this._prevColor = this.MyColor;
				this._prevText = this.MyText;
				this._prevBadge = this.GlobalBadge;
				PlayerList.UpdatePlayerRole(this._hub);
			}
		}
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
		NamedColor namedColor = this.NamedColors.FirstOrDefault((NamedColor row) => row.Name == this.MyColor);
		if (namedColor == null && i != "default")
		{
			this.SetColor("default");
		}
		else
		{
			this.CurrentColor = namedColor;
		}
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
		NamedColor namedColor = this.NamedColors.FirstOrDefault((NamedColor row) => row.Name == this.MyColor);
		if (namedColor != null)
		{
			this.CurrentColor = namedColor;
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
		this.RefreshPermissions(disp: true);
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
		this.TargetSetRemoteAdmin(open: true);
		this._hub.queryProcessor.SyncCommandsToClient();
	}

	[TargetRpc]
	private void TargetSetRemoteAdmin(bool open)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBool(open);
		this.SendTargetRPCInternal(null, "System.Void ServerRoles::TargetSetRemoteAdmin(System.Boolean)", -586263322, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	static ServerRoles()
	{
		ServerRoles.DictionarizedColorsCache = new Dictionary<string, NamedColor>();
		RemoteProcedureCalls.RegisterCommand(typeof(ServerRoles), "System.Void ServerRoles::CmdSetLocalTagPreferences(ServerRoles/BadgePreferences,ServerRoles/BadgeVisibilityPreferences,ServerRoles/BadgeVisibilityPreferences,System.Boolean)", InvokeUserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean, requiresAuthority: true);
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::RpcResetFixed()", InvokeUserCode_RpcResetFixed);
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::TargetSetHiddenRole(Mirror.NetworkConnection,System.String)", InvokeUserCode_TargetSetHiddenRole__NetworkConnection__String);
		RemoteProcedureCalls.RegisterRpc(typeof(ServerRoles), "System.Void ServerRoles::TargetSetRemoteAdmin(System.Boolean)", InvokeUserCode_TargetSetRemoteAdmin__Boolean);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetSetHiddenRole__NetworkConnection__String(NetworkConnection connection, string role)
	{
		if (!base.isServer)
		{
			if (string.IsNullOrEmpty(role))
			{
				this.SetColor(null);
				this.SetText(null);
				this.FixedBadge = null;
				this.SetText(null);
			}
			else
			{
				this.SetColor("silver");
				this.FixedBadge = Misc.SanitizeRichText(role.Replace("[", string.Empty).Replace("]", string.Empty)) + " " + TranslationReader.Get("Legacy_Interfaces", 18, "(hidden)");
				this.SetText(this.FixedBadge);
			}
		}
	}

	protected static void InvokeUserCode_TargetSetHiddenRole__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetHiddenRole called on server.");
		}
		else
		{
			((ServerRoles)obj).UserCode_TargetSetHiddenRole__NetworkConnection__String(null, reader.ReadString());
		}
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
		}
		else
		{
			((ServerRoles)obj).UserCode_RpcResetFixed();
		}
	}

	protected void UserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(BadgePreferences userPreferences, BadgeVisibilityPreferences globalPreferences, BadgeVisibilityPreferences localPreferences, bool refresh)
	{
		this.UserBadgePreferences = userPreferences;
		this._globalBadgeVisibilityPreferences = globalPreferences;
		this._localBadgeVisibilityPreferences = localPreferences;
		if (!refresh)
		{
			return;
		}
		if (userPreferences == BadgePreferences.PreferGlobal && this.HasGlobalBadge)
		{
			this.RefreshGlobalBadgeVisibility(globalPreferences);
		}
		else if (this.Group == null)
		{
			if (this.HasGlobalBadge)
			{
				this.RefreshGlobalBadgeVisibility(globalPreferences);
			}
		}
		else
		{
			this.RefreshLocalBadgeVisibility(localPreferences);
		}
	}

	protected static void InvokeUserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetLocalTagPreferences called on client.");
		}
		else
		{
			((ServerRoles)obj).UserCode_CmdSetLocalTagPreferences__BadgePreferences__BadgeVisibilityPreferences__BadgeVisibilityPreferences__Boolean(GeneratedNetworkCode._Read_ServerRoles_002FBadgePreferences(reader), GeneratedNetworkCode._Read_ServerRoles_002FBadgeVisibilityPreferences(reader), GeneratedNetworkCode._Read_ServerRoles_002FBadgeVisibilityPreferences(reader), reader.ReadBool());
		}
	}

	protected void UserCode_TargetSetRemoteAdmin__Boolean(bool open)
	{
	}

	protected static void InvokeUserCode_TargetSetRemoteAdmin__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSetRemoteAdmin called on server.");
		}
		else
		{
			((ServerRoles)obj).UserCode_TargetSetRemoteAdmin__Boolean(reader.ReadBool());
		}
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
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteString(this._myText);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteString(this._myColor);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteString(this.GlobalBadge);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteString(this.GlobalBadgeSignature);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._myText, SetTextHook, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this._myColor, SetColorHook, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this.GlobalBadge, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this.GlobalBadgeSignature, null, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._myText, SetTextHook, reader.ReadString());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._myColor, SetColorHook, reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.GlobalBadge, null, reader.ReadString());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.GlobalBadgeSignature, null, reader.ReadString());
		}
	}
}
