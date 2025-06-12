using System;
using System.Collections.Generic;
using System.Threading;
using CentralAuth;
using Cryptography;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib;
using Security;
using UnityEngine;
using Utf8Json;

public class CheaterReport : NetworkBehaviour
{
	private static readonly Dictionary<char, string> CharacterReplacements;

	internal static bool SendReportsByWebhooks;

	internal static string WebhookUrl;

	internal static string WebhookUsername;

	internal static string WebhookAvatar;

	internal static string ServerName;

	internal static string ReportHeader;

	internal static string ReportContent;

	internal static int WebhookColor;

	private int _reportedPlayersAmount;

	private float _lastReport;

	private HashSet<uint> _reportedPlayers;

	private RateLimit _commandRateLimit;

	private ReferenceHub _hub;

	private void Start()
	{
		this._hub = ReferenceHub.GetHub(this);
		this._commandRateLimit = this._hub.playerRateLimitHandler.RateLimits[1];
	}

	[Command(channel = 4)]
	private void CmdReport(uint playerNetId, string reason, byte[] signature, bool notifyGm)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteUInt(playerNetId);
		writer.WriteString(reason);
		writer.WriteBytesAndSize(signature);
		writer.WriteBool(notifyGm);
		base.SendCommandInternal("System.Void CheaterReport::CmdReport(System.UInt32,System.String,System.Byte[],System.Boolean)", -1325630461, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	private void IssueReport(GameConsoleTransmission reporter, string reporterUserId, string reportedUserId, string reportedAuth, string reportedIp, string reporterAuth, string reporterIp, ref string reason, ref byte[] signature, string reporterPublicKey, uint reportedNetId, string reporterNickname, string reportedNickname)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CheaterReport::IssueReport(GameConsoleTransmission,System.String,System.String,System.String,System.String,System.String,System.String,System.String&,System.Byte[]&,System.String,System.UInt32,System.String,System.String)' called when server was not active");
			return;
		}
		try
		{
			string data = $"reporterAuth={StringUtils.Base64Encode(reporterAuth)}&reporterIp={reporterIp}&reportedAuth={StringUtils.Base64Encode(reportedAuth)}&reportedIp={reportedIp}&reason={StringUtils.Base64Encode(CheaterReport.ConvertToLatin(reason))}&signature={Convert.ToBase64String(signature)}&reporterKey={StringUtils.Base64Encode(reporterPublicKey)}&token={ServerConsole.Password}&port={ServerConsole.PortToReport}&serverIp={ServerConsole.Ip}";
			string text = HttpQuery.Post(CentralServer.StandardUrl + "ingamereport.php", data);
			if (reporter == null)
			{
				return;
			}
			switch (text)
			{
			case "OK":
				this._reportedPlayers.Add(reportedNetId);
				reporter.SendToClient("[REPORTING] Player report successfully sent.", "green");
				break;
			case "ReportedUserIDAlreadyReported":
				reporter.SendToClient("[REPORTING] A report for this User ID already exists!" + Environment.NewLine, "yellow");
				break;
			case "RateLimited":
				reporter.SendToClient("[REPORTING] You are Ratelimited! Try again tomorrow." + Environment.NewLine, "red");
				break;
			default:
				reporter.SendToClient("[REPORTING] Error during **PROCESSING** player report:" + Environment.NewLine + text, "red");
				break;
			}
		}
		catch (Exception ex)
		{
			GameCore.Console.AddLog("[HOST] Error during **SENDING** player report:" + Environment.NewLine + ex.Message, Color.red);
			if (reporter == null)
			{
				return;
			}
			reporter.SendToClient("[REPORTING] Error during **SENDING** player report.", "yellow");
		}
		if (CheaterReport.SendReportsByWebhooks)
		{
			this.LogReport(reporter, reporterUserId, reportedUserId, ref reason, reportedNetId, notifyGm: true, reporterNickname, reportedNickname);
		}
	}

	[Server]
	private void LogReport(GameConsoleTransmission reporter, string reporterUserId, string reportedUserId, ref string reason, uint reportedNetId, bool notifyGm, string reporterNickname, string reportedNickname)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CheaterReport::LogReport(GameConsoleTransmission,System.String,System.String,System.String&,System.UInt32,System.Boolean,System.String,System.String)' called when server was not active");
		}
		else if (CheaterReport.SubmitReport(reporterUserId, reportedUserId, reason, reportedNetId, reporterNickname, reportedNickname, friendlyFire: false))
		{
			if (!notifyGm)
			{
				this._reportedPlayers.Add(reportedNetId);
				reporter.SendToClient("[REPORTING] Player report successfully sent to local administrators by webhooks.", "green");
			}
		}
		else
		{
			reporter.SendToClient("[REPORTING] Failed to send report to local administrators by webhooks.", "red");
		}
	}

	[Server]
	private void SendStaffChatNotification(string reporterUserId, string reportedUserId, string reason, string reporterNickname, string reportedNickname)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CheaterReport::SendStaffChatNotification(System.String,System.String,System.String,System.String,System.String)' called when server was not active");
			return;
		}
		string content = "0!<align=center><color=red><u>REPORT RECEIVED</u></color></align>\n<color=yellow>Reporter:</color>\n" + reporterNickname + " (" + reporterUserId + ")\n<color=yellow>Reported:</color>\n" + reportedNickname + " (" + reportedUserId + ")\n<color=yellow>Reason:</color>\n" + reason;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			ClientInstanceMode mode = allHub.Mode;
			if (mode != ClientInstanceMode.Unverified && mode != ClientInstanceMode.DedicatedServer && allHub.serverRoles.AdminChatPerms)
			{
				allHub.encryptedChannelManager.TrySendMessageToClient(content, EncryptedChannelManager.EncryptedChannel.AdminChat);
			}
		}
	}

	internal static bool SubmitReport(string reporterUserId, string reportedUserId, string reason, uint reportedId, string reporterNickname, string reportedNickname, bool friendlyFire)
	{
		try
		{
			HttpQuery.Post(friendlyFire ? FriendlyFireConfig.WebhookUrl : CheaterReport.WebhookUrl, "payload_json=" + JsonSerializer.ToJsonString(new DiscordWebhook(string.Empty, CheaterReport.WebhookUsername, CheaterReport.WebhookAvatar, tts: false, new DiscordEmbed[1]
			{
				new DiscordEmbed(CheaterReport.ReportHeader, "rich", CheaterReport.ReportContent, CheaterReport.WebhookColor, new DiscordEmbedField[10]
				{
					new DiscordEmbedField("Server Name", CheaterReport.ServerName, inline: false),
					new DiscordEmbedField("Server Endpoint", $"{ServerConsole.Ip}:{ServerConsole.PortToReport}", inline: false),
					new DiscordEmbedField("Reporter UserID", CheaterReport.AsDiscordCode(reporterUserId), inline: false),
					new DiscordEmbedField("Reporter Nickname", CheaterReport.DiscordSanitize(reporterNickname), inline: false),
					new DiscordEmbedField("Reported UserID", CheaterReport.AsDiscordCode(reportedUserId), inline: false),
					new DiscordEmbedField("Reported Nickname", CheaterReport.DiscordSanitize(reportedNickname), inline: false),
					new DiscordEmbedField("Reported NetID", reportedId.ToString(), inline: false),
					new DiscordEmbedField("Reason", CheaterReport.DiscordSanitize(reason), inline: false),
					new DiscordEmbedField("Timestamp", TimeBehaviour.Rfc3339Time(), inline: false),
					new DiscordEmbedField("UTC Timestamp", TimeBehaviour.Rfc3339Time(DateTimeOffset.UtcNow), inline: false)
				})
			})));
			return true;
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Failed to send report by webhook: " + ex.Message);
			Debug.LogException(ex);
			return false;
		}
	}

	private static string ConvertToLatin(string str)
	{
		foreach (KeyValuePair<char, string> characterReplacement in CheaterReport.CharacterReplacements)
		{
			str = str.Replace(characterReplacement.Key.ToString(), characterReplacement.Value);
		}
		return str;
	}

	private static string AsDiscordCode(string text)
	{
		return "`" + text.Replace("`", "'") + "`";
	}

	private static string DiscordSanitize(string text)
	{
		return text.Replace("<", "(").Replace(">", ")").Replace("@", "@ ")
			.Replace("`", "'")
			.Replace("~~", "∼∼")
			.Replace("*", "★")
			.Replace("_", "\uff3f")
			.Replace("&", " [AMP] ")
			.Replace("?", " [QM] ");
	}

	static CheaterReport()
	{
		CheaterReport.CharacterReplacements = new Dictionary<char, string>
		{
			{ 'а', "a" },
			{ 'б', "b" },
			{ 'в', "v" },
			{ 'г', "g" },
			{ 'д', "d" },
			{ 'е', "e" },
			{ 'ё', "yo" },
			{ 'ж', "zh" },
			{ 'з', "z" },
			{ 'и', "i" },
			{ 'й', "j" },
			{ 'к', "k" },
			{ 'л', "l" },
			{ 'м', "m" },
			{ 'н', "n" },
			{ 'о', "o" },
			{ 'п', "p" },
			{ 'р', "r" },
			{ 'с', "s" },
			{ 'т', "t" },
			{ 'у', "u" },
			{ 'ф', "f" },
			{ 'х', "h" },
			{ 'ц', "c" },
			{ 'ч', "ch" },
			{ 'ш', "sh" },
			{ 'щ', "sch" },
			{ 'ъ', "j" },
			{ 'ы', "i" },
			{ 'ь', "j" },
			{ 'э', "e" },
			{ 'ю', "yu" },
			{ 'я', "ya" },
			{ 'А', "A" },
			{ 'Б', "B" },
			{ 'В', "V" },
			{ 'Г', "G" },
			{ 'Д', "D" },
			{ 'Е', "E" },
			{ 'Ё', "Yo" },
			{ 'Ж', "Zh" },
			{ 'З', "Z" },
			{ 'И', "I" },
			{ 'Й', "J" },
			{ 'К', "K" },
			{ 'Л', "L" },
			{ 'М', "M" },
			{ 'Н', "N" },
			{ 'О', "O" },
			{ 'П', "P" },
			{ 'Р', "R" },
			{ 'С', "S" },
			{ 'Т', "T" },
			{ 'У', "U" },
			{ 'Ф', "F" },
			{ 'Х', "H" },
			{ 'Ц', "C" },
			{ 'Ч', "Ch" },
			{ 'Ш', "Sh" },
			{ 'Щ', "Sch" },
			{ 'Ъ', "J" },
			{ 'Ы', "I" },
			{ 'Ь', "J" },
			{ 'Э', "E" },
			{ 'Ю', "Yu" },
			{ 'Я', "Ya" }
		};
		CheaterReport.SendReportsByWebhooks = false;
		RemoteProcedureCalls.RegisterCommand(typeof(CheaterReport), "System.Void CheaterReport::CmdReport(System.UInt32,System.String,System.Byte[],System.Boolean)", InvokeUserCode_CmdReport__UInt32__String__Byte_005B_005D__Boolean, requiresAuthority: true);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdReport__UInt32__String__Byte_005B_005D__Boolean(uint playerNetId, string reason, byte[] signature, bool notifyGm)
	{
		if (!this._commandRateLimit.CanExecute() || reason == null)
		{
			return;
		}
		float num = Time.time - this._lastReport;
		if (num < 2f)
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Reporting rate limit exceeded (1).", "red");
			return;
		}
		if (num > 60f)
		{
			this._reportedPlayersAmount = 0;
		}
		if (this._reportedPlayersAmount > 5)
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Reporting rate limit exceeded (2).", "red");
			return;
		}
		if (notifyGm && (!CustomNetworkManager.IsVerified || string.IsNullOrEmpty(ServerConsole.Password)))
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Server is not verified - you can't use report feature on this server.", "red");
			return;
		}
		if (!ReferenceHub.TryGetHubNetID(playerNetId, out var hub))
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Can't find player with that PlayerID.", "red");
			return;
		}
		PlayerAuthenticationManager reportedPam = hub.authManager;
		if (this._reportedPlayers == null)
		{
			this._reportedPlayers = new HashSet<uint>();
		}
		if (this._reportedPlayers.Contains(playerNetId))
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] You have already reported that player.", "red");
			return;
		}
		if (string.IsNullOrEmpty(reportedPam.UserId))
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Failed: User ID of reported player is null.", "red");
			return;
		}
		if (string.IsNullOrEmpty(this._hub.authManager.UserId))
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Failed: your User ID of is null.", "red");
			return;
		}
		if (this._hub.authManager.UserId == reportedPam.UserId)
		{
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] You can't report yourself!" + Environment.NewLine, "yellow");
			return;
		}
		string reportedNickname = hub.nicknameSync.MyNick;
		if (!notifyGm)
		{
			PlayerReportingPlayerEventArgs e = new PlayerReportingPlayerEventArgs(this._hub, hub, reason);
			PlayerEvents.OnReportingPlayer(e);
			if (!e.IsAllowed)
			{
				return;
			}
			reason = e.Reason;
			GameCore.Console.AddLog("Player " + this._hub.LoggedNameFromRefHub() + " reported player " + hub.LoggedNameFromRefHub() + " with reason " + reason + ".", Color.gray);
			this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Player report successfully sent to local administrators.", "green");
			this.SendStaffChatNotification(this._hub.authManager.UserId, reportedPam.UserId, reason, this._hub.nicknameSync.MyNick, reportedNickname);
			if (CheaterReport.SendReportsByWebhooks)
			{
				Thread thread = new Thread((ThreadStart)delegate
				{
					this.LogReport(this._hub.gameConsoleTransmission, this._hub.authManager.UserId, reportedPam.UserId, ref reason, playerNetId, notifyGm: false, this._hub.nicknameSync.MyNick, reportedNickname);
				});
				thread.Priority = System.Threading.ThreadPriority.Lowest;
				thread.IsBackground = true;
				thread.Name = "Reporting player (locally) - " + reportedPam.UserId + " by " + this._hub.authManager.UserId;
				thread.Start();
			}
			PlayerEvents.OnReportedPlayer(new PlayerReportedPlayerEventArgs(this._hub, hub, reason));
		}
		else
		{
			if (signature == null)
			{
				return;
			}
			if (!ECDSA.VerifyBytes(reportedPam.SyncedUserId + ";" + CheaterReport.ConvertToLatin(reason), signature, this._hub.authManager.AuthenticationResponse.PublicKey))
			{
				this._hub.gameConsoleTransmission.SendToClient("[REPORTING] Invalid report signature.", "red");
				return;
			}
			PlayerReportingCheaterEventArgs e2 = new PlayerReportingCheaterEventArgs(this._hub, hub, reason);
			PlayerEvents.OnReportingCheater(e2);
			if (e2.IsAllowed)
			{
				reason = e2.Reason;
				this._lastReport = Time.time;
				this._reportedPlayersAmount++;
				GameCore.Console.AddLog("Player " + this._hub.LoggedNameFromRefHub() + " reported player " + hub.LoggedNameFromRefHub() + " with reason " + reason + ". Sending report to Global Moderation.", Color.gray);
				this.SendStaffChatNotification(this._hub.authManager.UserId, reportedPam.UserId, reason, this._hub.nicknameSync.MyNick, reportedNickname);
				Thread thread2 = new Thread((ThreadStart)delegate
				{
					this.IssueReport(this._hub.gameConsoleTransmission, this._hub.authManager.UserId, reportedPam.UserId, reportedPam.GetAuthToken(), reportedPam.connectionToClient.address, this._hub.authManager.GetAuthToken(), this._hub.connectionToClient.address, ref reason, ref signature, ECDSA.KeyToString(this._hub.authManager.AuthenticationResponse.PublicKey), playerNetId, this._hub.nicknameSync.MyNick, reportedNickname);
				});
				thread2.Priority = System.Threading.ThreadPriority.Lowest;
				thread2.IsBackground = true;
				thread2.Name = "Reporting player - " + reportedPam.UserId + " by " + this._hub.authManager.UserId;
				thread2.Start();
				PlayerEvents.OnReportedCheater(new PlayerReportedCheaterEventArgs(this._hub, hub, reason));
			}
		}
	}

	protected static void InvokeUserCode_CmdReport__UInt32__String__Byte_005B_005D__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReport called on client.");
		}
		else
		{
			((CheaterReport)obj).UserCode_CmdReport__UInt32__String__Byte_005B_005D__Boolean(reader.ReadUInt(), reader.ReadString(), reader.ReadBytesAndSize(), reader.ReadBool());
		}
	}
}
