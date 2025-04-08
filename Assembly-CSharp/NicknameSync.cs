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
				string firstNickname = this._firstNickname;
				int num = 48 - 0;
				this._firstNickname = firstNickname.Substring(0, num);
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
			string text2;
			if (value.Length <= 48)
			{
				text2 = text;
			}
			else
			{
				string text3 = text;
				int num = 48 - 0;
				text2 = text3.Substring(0, num);
			}
			this.Network_myNickSync = text2;
			if (!NetworkServer.active)
			{
				return;
			}
			this.NickSet = true;
			this._firstNickname = this._myNickSync;
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
			PlayerChangingNicknameEventArgs playerChangingNicknameEventArgs = new PlayerChangingNicknameEventArgs(this._hub, this._displayName, value);
			PlayerEvents.OnChangingNickname(playerChangingNicknameEventArgs);
			if (!playerChangingNicknameEventArgs.IsAllowed)
			{
				return;
			}
			string cleanDisplayName = this._cleanDisplayName;
			string newNickname = playerChangingNicknameEventArgs.NewNickname;
			this.Network_displayName = newNickname;
			this.UpdatePlayerlistInstance(null, this._displayName);
			PlayerEvents.OnChangedNickname(new PlayerChangedNicknameEventArgs(this._hub, cleanDisplayName, this._displayName));
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
			if (!NetworkServer.active)
			{
				return;
			}
			this.Network_playerInfoToShow = value;
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
			if (!NetworkServer.active)
			{
				return;
			}
			this.Network_customPlayerInfoString = value;
		}
	}

	public bool NickSet { get; private set; }

	public bool HasCustomName
	{
		get
		{
			return this._cleanDisplayName != null;
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
			list.AddRange(info.Split('<', StringSplitOptions.None));
		}
		if (flag2)
		{
			list.AddRange(info.Split("\\u003c", StringSplitOptions.None));
		}
		bool flag3 = true;
		foreach (string text in list)
		{
			if (!text.StartsWith("/", StringComparison.Ordinal) && !text.StartsWith("b>", StringComparison.Ordinal) && !text.StartsWith("i>", StringComparison.Ordinal) && !text.StartsWith("size=", StringComparison.Ordinal) && text.Length != 0)
			{
				if (text.StartsWith("color=", StringComparison.Ordinal))
				{
					if (text.Length < 14)
					{
						rejectionText = "Provided color tag doesn't match the requirements - Invalid color";
						flag3 = false;
						break;
					}
					if (text[13] != '>')
					{
						rejectionText = "Provided color tag doesn't match the requirements - unclosed color tag (missing '>')";
						flag3 = false;
						break;
					}
					if (!Misc.AcceptedColours.Contains(text.Substring(7, 6)))
					{
						rejectionText = "Provided color tag doesn't match the requirements - This color is not from allowed list";
						flag3 = false;
						break;
					}
				}
				else
				{
					if (!text.StartsWith("#", StringComparison.Ordinal))
					{
						rejectionText = "Provided text has rich text tag which is not allowed";
						flag3 = false;
						break;
					}
					if (text.Length < 8)
					{
						rejectionText = "Provided color tag doesn't match the requirements - Invalid color";
						flag3 = false;
						break;
					}
					if (text[7] != '>')
					{
						rejectionText = "Provided color tag doesn't match the requirements - unclosed color tag (missing '>')";
						flag3 = false;
						break;
					}
					if (!Misc.AcceptedColours.Contains(text.Substring(1, 6)))
					{
						rejectionText = "Provided color tag doesn't match the requirements - This color is not from allowed list";
						flag3 = false;
						break;
					}
				}
			}
		}
		ListPool<string>.Shared.Return(list);
		return flag3;
	}

	public static void WriteDefaultInfo(ReferenceHub owner, StringBuilder sb, out Color texColor, PlayerInfoArea? flagsOverride = null)
	{
		texColor = owner.roleManager.CurrentRole.RoleColor;
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
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing null name.", ConsoleColor.Gray, false);
			BanPlayer.BanUser(this._hub, "Null name", 1577847600L);
			this.SetNick("(null)");
			return;
		}
		if (n.Length > 1024)
		{
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing a too long name.", ConsoleColor.Gray, false);
			BanPlayer.BanUser(this._hub, "Too long name", 1577847600L);
			this.SetNick("(too long)");
			return;
		}
		bool flag;
		string text = this.CleanNickName(n, out flag);
		if (!flag)
		{
			ServerConsole.AddLog("Kicked " + base.connectionToClient.address + " for having an empty name.", ConsoleColor.Gray, false);
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
			string text = ConfigFile.ServerConfig.GetString("nickname_filter", "") ?? string.Empty;
			if (!string.IsNullOrEmpty(text))
			{
				this._nickFilter = new Regex(text, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500.0));
				this._replacement = ConfigFile.ServerConfig.GetString("nickname_filter_replacement", "") ?? string.Empty;
			}
		}
		if (!base.isLocalPlayer)
		{
			return;
		}
		this.SetNick("Dedicated Server");
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
			this._cleanDisplayName = Misc.SanitizeRichText(username.Replace("\n", string.Empty).Replace("\r", string.Empty), "", "").Trim();
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
		ReferenceHub referenceHub;
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub) && referenceHub.roleManager.CurrentRole is IFpcRole)
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
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(n);
		base.SendCommandInternal("System.Void NicknameSync::CmdSetNick(System.String)", -1313630199, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
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
			Regex nickFilter = this._nickFilter;
			text = ((nickFilter != null) ? nickFilter.Replace(nick, this._replacement) : null) ?? nick;
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog(string.Format("Error when filtering nick {0}: {1}", nick, ex), ConsoleColor.Gray, false);
			text = "(filter failed)";
		}
		if (nick != text)
		{
			this.DisplayName = text;
		}
		if (base.isLocalPlayer && ServerStatic.IsDedicated)
		{
			return;
		}
		ServerConsole.AddLog(string.Concat(new string[]
		{
			"Nickname of ",
			this._hub.authManager.UserId,
			" is now ",
			nick,
			"."
		}), ConsoleColor.Gray, false);
		ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Concat(new string[]
		{
			"Nickname of ",
			this._hub.authManager.UserId,
			" is now ",
			nick,
			"."
		}), ServerLogs.ServerLogType.ConnectionUpdate, false);
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

	public float NetworkViewRange
	{
		get
		{
			return this.ViewRange;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<float>(value, ref this.ViewRange, 1UL, null);
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
			base.GeneratedSyncVarSetter<string>(value, ref this._customPlayerInfoString, 2UL, new Action<string, string>(this.SetCustomInfo));
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
			base.GeneratedSyncVarSetter<PlayerInfoArea>(value, ref this._playerInfoToShow, 4UL, null);
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
			base.GeneratedSyncVarSetter<string>(value, ref this._myNickSync, 8UL, new Action<string, string>(this.UpdatePlayerlistInstance));
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
			base.GeneratedSyncVarSetter<string>(value, ref this._displayName, 16UL, new Action<string, string>(this.UpdateCustomName));
		}
	}

	protected void UserCode_CmdSetNick__String(string n)
	{
		if (base.isLocalPlayer)
		{
			this.MyNick = n;
			return;
		}
		if (this.NickSet)
		{
			return;
		}
		if (PlayerAuthenticationManager.OnlineMode)
		{
			return;
		}
		this.NickSet = true;
		if (n == null)
		{
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing null name.", ConsoleColor.Gray, false);
			BanPlayer.BanUser(this._hub, "Null name", 1577847600L);
			this.SetNick("(null)");
			return;
		}
		if (n.Length > 1024)
		{
			ServerConsole.AddLog("Banned " + base.connectionToClient.address + " for passing a too long name.", ConsoleColor.Gray, false);
			BanPlayer.BanUser(this._hub, "Too long name", 1577847600L);
			this.SetNick("(too long)");
			return;
		}
		bool flag;
		string text = this.CleanNickName(n, out flag);
		if (!flag)
		{
			ServerConsole.AddLog("Kicked " + base.connectionToClient.address + " for having an empty name.", ConsoleColor.Gray, false);
			ServerConsole.Disconnect(base.connectionToClient, "You may not have an empty name.");
			this.SetNick("Empty Name");
			return;
		}
		text = Misc.SanitizeRichText(text, "＜", "＞");
		text = text.Replace("[", "(");
		text = text.Replace("]", ")");
		if (text.Length > 48)
		{
			string text2 = text;
			int num = 48 - 0;
			text = text2.Substring(0, num);
		}
		this.SetNick(text);
		this._hub.characterClassManager.SyncServerCmdBinding();
		PlayerEvents.OnJoined(new PlayerJoinedEventArgs(this._hub));
	}

	protected static void InvokeUserCode_CmdSetNick__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNick called on client.");
			return;
		}
		((NicknameSync)obj).UserCode_CmdSetNick__String(reader.ReadString());
	}

	static NicknameSync()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(NicknameSync), "System.Void NicknameSync::CmdSetNick(System.String)", new RemoteCallDelegate(NicknameSync.InvokeUserCode_CmdSetNick__String), true);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this.ViewRange);
			writer.WriteString(this._customPlayerInfoString);
			global::Mirror.GeneratedNetworkCode._Write_PlayerInfoArea(writer, this._playerInfoToShow);
			writer.WriteString(this._myNickSync);
			writer.WriteString(this._displayName);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteFloat(this.ViewRange);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteString(this._customPlayerInfoString);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			global::Mirror.GeneratedNetworkCode._Write_PlayerInfoArea(writer, this._playerInfoToShow);
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteString(this._myNickSync);
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteString(this._displayName);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.ViewRange, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize<string>(ref this._customPlayerInfoString, new Action<string, string>(this.SetCustomInfo), reader.ReadString());
			base.GeneratedSyncVarDeserialize<PlayerInfoArea>(ref this._playerInfoToShow, null, global::Mirror.GeneratedNetworkCode._Read_PlayerInfoArea(reader));
			base.GeneratedSyncVarDeserialize<string>(ref this._myNickSync, new Action<string, string>(this.UpdatePlayerlistInstance), reader.ReadString());
			base.GeneratedSyncVarDeserialize<string>(ref this._displayName, new Action<string, string>(this.UpdateCustomName), reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.ViewRange, null, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._customPlayerInfoString, new Action<string, string>(this.SetCustomInfo), reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<PlayerInfoArea>(ref this._playerInfoToShow, null, global::Mirror.GeneratedNetworkCode._Read_PlayerInfoArea(reader));
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._myNickSync, new Action<string, string>(this.UpdatePlayerlistInstance), reader.ReadString());
		}
		if ((num & 16L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this._displayName, new Action<string, string>(this.UpdateCustomName), reader.ReadString());
		}
	}

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
}
