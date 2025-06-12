using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CentralAuth;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using UnityEngine;

public class NicknameSync : NetworkBehaviour
{
	public const string CustomInfoRejectionRegexFail = "Provided text doesn't match the PlayerCustomInfo regex.";

	public const string CustomInfoRejectionInvalidTextColor = "Provided color tag doesn't match the requirements - Invalid color";

	public const string CustomInfoRejectionUnclosedColorTag = "Provided color tag doesn't match the requirements - unclosed color tag (missing '>')";

	public const string CustomInfoRejectionColorNotAllowed = "Provided color tag doesn't match the requirements - This color is not from allowed list";

	public const string CustomInfoRejectionTagNotAllowed = "Provided text has rich text tag which is not allowed";

	private const ushort MaxNicknameLen = 48;

	private const float TextVisiblitySpeedChange = 3f;

	public LayerMask RaycastMask;

	[SyncVar]
	public float ViewRange;

	private ReferenceHub _hub;

	[SyncVar(hook = "SetCustomInfo")]
	private string _customPlayerInfoString;

	[SyncVar]
	private PlayerInfoArea _playerInfoToShow = (PlayerInfoArea)(-1);

	[SyncVar(hook = "UpdatePlayerlistInstance")]
	private string _myNickSync;

	private string _firstNickname;

	[SyncVar(hook = "UpdateCustomName")]
	private string _displayName;

	private string _cleanDisplayName;

	private Regex _nickFilter;

	private string _replacement;

	public string CombinedName
	{
		get
		{
			if (!this.HasCustomName)
			{
				return this.MyNick;
			}
			return this._cleanDisplayName + " (" + this.MyNick + ")";
		}
	}

	public string MyNick
	{
		get
		{
			if (this.NickSet)
			{
				return this._firstNickname;
			}
			if (this._myNickSync == null)
			{
				return "(null)";
			}
			this.NickSet = true;
			this._firstNickname = Misc.SanitizeRichText(this._myNickSync.Replace("\n", string.Empty).Replace("\r", string.Empty), "＜", "＞");
			if (this._firstNickname.Length > 48)
			{
				this._firstNickname = this._firstNickname.Substring(0, 48);
			}
			return this._firstNickname;
		}
		set
		{
			if (value == null)
			{
				value = "(null)";
			}
			string text = Misc.SanitizeRichText(value, "＜", "＞");
			this.Network_myNickSync = ((value.Length > 48) ? text.Substring(0, 48) : text);
			if (NetworkServer.active)
			{
				this.NickSet = true;
				this._firstNickname = this._myNickSync;
			}
		}
	}

	public string DisplayName
	{
		get
		{
			return (this.HasCustomName ? this._cleanDisplayName : this.MyNick) ?? string.Empty;
		}
		set
		{
			string newNickname = value;
			PlayerChangingNicknameEventArgs e = new PlayerChangingNicknameEventArgs(this._hub, this._displayName, newNickname);
			PlayerEvents.OnChangingNickname(e);
			if (e.IsAllowed)
			{
				string cleanDisplayName = this._cleanDisplayName;
				newNickname = e.NewNickname;
				this.Network_displayName = newNickname;
				this.UpdatePlayerlistInstance(null, this._displayName);
				PlayerEvents.OnChangedNickname(new PlayerChangedNicknameEventArgs(this._hub, cleanDisplayName, this._displayName));
			}
		}
	}

	public PlayerInfoArea ShownPlayerInfo
	{
		get
		{
			return this._playerInfoToShow;
		}
		set
		{
			if (NetworkServer.active)
			{
				this.Network_playerInfoToShow = value;
			}
		}
	}

	public string CustomPlayerInfo
	{
		get
		{
			return this._customPlayerInfoString;
		}
		set
		{
			if (NetworkServer.active)
			{
				this.Network_customPlayerInfoString = value;
			}
		}
	}

	public bool NickSet { get; private set; }

	public bool HasCustomName => this._cleanDisplayName != null;

	public float NetworkViewRange
	{
		get
		{
			return this.ViewRange;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ViewRange, 1uL, null);
		}
	}

	public string Network_customPlayerInfoString
	{
		get
		{
			return this._customPlayerInfoString;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._customPlayerInfoString, 2uL, SetCustomInfo);
		}
	}

	public PlayerInfoArea Network_playerInfoToShow
	{
		get
		{
			return this._playerInfoToShow;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._playerInfoToShow, 4uL, null);
		}
	}

	public string Network_myNickSync
	{
		get
		{
			return this._myNickSync;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._myNickSync, 8uL, UpdatePlayerlistInstance);
		}
	}

	public string Network_displayName
	{
		get
		{
			return this._displayName;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._displayName, 16uL, UpdateCustomName);
		}
	}

	public static bool ValidateCustomInfo(string info, out string rejectionText)
	{
		rejectionText = string.Empty;
		if (string.IsNullOrEmpty(info))
		{
			return true;
		}
		if (!Misc.PlayerCustomInfoRegex.IsMatch(info))
		{
			rejectionText = "Provided text doesn't match the PlayerCustomInfo regex.";
			return false;
		}
		bool flag = info.Contains("<");
		bool flag2 = info.Contains("\\u003c");
		if (!flag && !flag2)
		{
			return true;
		}
		List<string> list = ListPool<string>.Shared.Rent();
		if (flag)
		{
			list.AddRange(info.Split('<'));
		}
		if (flag2)
		{
			list.AddRange(info.Split("\\u003c"));
		}
		bool result = true;
		foreach (string item in list)
		{
			if (item.StartsWith("/", StringComparison.Ordinal) || item.StartsWith("b>", StringComparison.Ordinal) || item.StartsWith("i>", StringComparison.Ordinal) || item.StartsWith("size=", StringComparison.Ordinal) || item.Length == 0)
			{
				continue;
			}
			if (item.StartsWith("color=", StringComparison.Ordinal))
			{
				if (item.Length < 14)
				{
					rejectionText = "Provided color tag doesn't match the requirements - Invalid color";
					result = false;
					break;
				}
				if (item[13] != '>')
				{
					rejectionText = "Provided color tag doesn't match the requirements - unclosed color tag (missing '>')";
					result = false;
					break;
				}
				if (!Misc.AcceptedColours.Contains<string>(item.Substring(7, 6)))
				{
					rejectionText = "Provided color tag doesn't match the requirements - This color is not from allowed list";
					result = false;
					break;
				}
				continue;
			}
			if (item.StartsWith("#", StringComparison.Ordinal))
			{
				if (item.Length < 8)
				{
					rejectionText = "Provided color tag doesn't match the requirements - Invalid color";
					result = false;
					break;
				}
				if (item[7] != '>')
				{
					rejectionText = "Provided color tag doesn't match the requirements - unclosed color tag (missing '>')";
					result = false;
					break;
				}
				if (!Misc.AcceptedColours.Contains<string>(item.Substring(1, 6)))
				{
					rejectionText = "Provided color tag doesn't match the requirements - This color is not from allowed list";
					result = false;
					break;
				}
				continue;
			}
			rejectionText = "Provided text has rich text tag which is not allowed";
			result = false;
			break;
		}
		ListPool<string>.Shared.Return(list);
		return result;
	}

	public static void WriteDefaultInfo(ReferenceHub owner, StringBuilder sb, PlayerInfoArea? flagsOverride = null)
	{
	}

	[ServerCallback]
	public void UpdateNickname(string n)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		this.NickSet = true;
		if (n == null)
		{
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing null name.");
			BanPlayer.BanUser(this._hub, "Null name", 1577847600L);
			this.SetNick("(null)");
			return;
		}
		if (n.Length > 1024)
		{
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing a too long name.");
			BanPlayer.BanUser(this._hub, "Too long name", 1577847600L);
			this.SetNick("(too long)");
			return;
		}
		bool printable;
		string text = this.CleanNickName(n, out printable);
		if (!printable)
		{
			ServerConsole.AddLog("Kicked " + base.connectionToClient.address + " for having an empty name.");
			ServerConsole.Disconnect(base.connectionToClient, "You may not have an empty name.");
			this.SetNick("Empty Name");
			return;
		}
		if (text.Length > 48)
		{
			text = text.Substring(0, 48);
		}
		this.SetNick(text);
	}

	private void Start()
	{
		this._hub = ReferenceHub.GetHub(base.gameObject);
		this._nickFilter = null;
		this._replacement = "";
		if (NetworkServer.active)
		{
			this.NetworkViewRange = ConfigFile.ServerConfig.GetFloat("player_info_range", 10f);
			string text = ConfigFile.ServerConfig.GetString("nickname_filter") ?? string.Empty;
			if (!string.IsNullOrEmpty(text))
			{
				this._nickFilter = new Regex(text, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500.0));
				this._replacement = ConfigFile.ServerConfig.GetString("nickname_filter_replacement") ?? string.Empty;
			}
		}
		if (base.isLocalPlayer)
		{
			this.SetNick("Dedicated Server");
		}
	}

	private void UpdatePlayerlistInstance(string p, string username)
	{
	}

	private void UpdateCustomName(string p, string username)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			this._cleanDisplayName = null;
		}
		else
		{
			this._cleanDisplayName = Misc.SanitizeRichText(username.Replace("\n", string.Empty).Replace("\r", string.Empty)).Trim();
			if (this._cleanDisplayName.Length > 48)
			{
				this._cleanDisplayName = this._cleanDisplayName.Substring(0, 48);
			}
			this._cleanDisplayName += "<color=#855439>*</color>";
		}
		this.UpdatePlayerlistInstance(p, username);
	}

	private bool TryGetRayTransform(out Transform tr)
	{
		if (this._hub.roleManager.CurrentRole is IFpcRole)
		{
			tr = this._hub.PlayerCameraReference;
			return true;
		}
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out var hub) && hub.roleManager.CurrentRole is IFpcRole)
		{
			tr = MainCameraController.CurrentCamera;
			return true;
		}
		tr = null;
		return false;
	}

	private void Update()
	{
	}

	private void SetCustomInfo(string oldValue, string newValue)
	{
	}

	[Command(channel = 4)]
	private void CmdSetNick(string n)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(n);
		base.SendCommandInternal("System.Void NicknameSync::CmdSetNick(System.String)", -1313630199, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	private void SetNick(string nick)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void NicknameSync::SetNick(System.String)' called when server was not active");
			return;
		}
		this.MyNick = nick;
		string text;
		try
		{
			text = this._nickFilter?.Replace(nick, this._replacement) ?? nick;
		}
		catch (Exception arg)
		{
			ServerConsole.AddLog($"Error when filtering nick {nick}: {arg}");
			text = "(filter failed)";
		}
		if (nick != text)
		{
			this.DisplayName = text;
		}
		if (!base.isLocalPlayer || !ServerStatic.IsDedicated)
		{
			ServerConsole.AddLog("Nickname of " + this._hub.authManager.UserId + " is now " + nick + ".");
			ServerLogs.AddLog(ServerLogs.Modules.Networking, "Nickname of " + this._hub.authManager.UserId + " is now " + nick + ".", ServerLogs.ServerLogType.ConnectionUpdate);
		}
	}

	private string CleanNickName(string input, out bool printable)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent(input.Length);
		char c = '0';
		printable = false;
		foreach (char c2 in input)
		{
			if (char.IsLetterOrDigit(c2) || char.IsPunctuation(c2) || char.IsSymbol(c2))
			{
				printable = true;
				stringBuilder.Append(c2);
			}
			else if (char.IsWhiteSpace(c2) && c2 != '\n' && c2 != '\r' && c2 != '\t')
			{
				stringBuilder.Append(c2);
			}
			else if (char.IsHighSurrogate(c2))
			{
				c = c2;
			}
			else if (char.IsLowSurrogate(c2) && char.IsSurrogatePair(c, c2))
			{
				stringBuilder.Append(c);
				stringBuilder.Append(c2);
				printable = true;
			}
		}
		return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdSetNick__String(string n)
	{
		if (base.isLocalPlayer)
		{
			this.MyNick = n;
		}
		else
		{
			if (this.NickSet || PlayerAuthenticationManager.OnlineMode)
			{
				return;
			}
			this.NickSet = true;
			if (n == null)
			{
				ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing null name.");
				BanPlayer.BanUser(this._hub, "Null name", 1577847600L);
				this.SetNick("(null)");
				return;
			}
			if (n.Length > 1024)
			{
				ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing a too long name.");
				BanPlayer.BanUser(this._hub, "Too long name", 1577847600L);
				this.SetNick("(too long)");
				return;
			}
			string content = this.CleanNickName(n, out var printable);
			if (!printable)
			{
				ServerConsole.AddLog("Kicked " + base.connectionToClient.address + " for having an empty name.");
				ServerConsole.Disconnect(base.connectionToClient, "You may not have an empty name.");
				this.SetNick("Empty Name");
				return;
			}
			content = Misc.SanitizeRichText(content, "＜", "＞");
			content = content.Replace("[", "(");
			content = content.Replace("]", ")");
			if (content.Length > 48)
			{
				content = content.Substring(0, 48);
			}
			this.SetNick(content);
			this._hub.characterClassManager.SyncServerCmdBinding();
			PlayerEvents.OnJoined(new PlayerJoinedEventArgs(this._hub));
		}
	}

	protected static void InvokeUserCode_CmdSetNick__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNick called on client.");
		}
		else
		{
			((NicknameSync)obj).UserCode_CmdSetNick__String(reader.ReadString());
		}
	}

	static NicknameSync()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(NicknameSync), "System.Void NicknameSync::CmdSetNick(System.String)", InvokeUserCode_CmdSetNick__String, requiresAuthority: true);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this.ViewRange);
			writer.WriteString(this._customPlayerInfoString);
			GeneratedNetworkCode._Write_PlayerInfoArea(writer, this._playerInfoToShow);
			writer.WriteString(this._myNickSync);
			writer.WriteString(this._displayName);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(this.ViewRange);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteString(this._customPlayerInfoString);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			GeneratedNetworkCode._Write_PlayerInfoArea(writer, this._playerInfoToShow);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteString(this._myNickSync);
		}
		if ((base.syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteString(this._displayName);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.ViewRange, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._customPlayerInfoString, SetCustomInfo, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this._playerInfoToShow, null, GeneratedNetworkCode._Read_PlayerInfoArea(reader));
			base.GeneratedSyncVarDeserialize(ref this._myNickSync, UpdatePlayerlistInstance, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this._displayName, UpdateCustomName, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ViewRange, null, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._customPlayerInfoString, SetCustomInfo, reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._playerInfoToShow, null, GeneratedNetworkCode._Read_PlayerInfoArea(reader));
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._myNickSync, UpdatePlayerlistInstance, reader.ReadString());
		}
		if ((num & 0x10L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._displayName, UpdateCustomName, reader.ReadString());
		}
	}
}
