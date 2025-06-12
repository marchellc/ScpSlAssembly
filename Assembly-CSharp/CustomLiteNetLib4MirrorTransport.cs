using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CentralAuth;
using Cryptography;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LiteNetLib;
using LiteNetLib.Utils;
using Mirror.LiteNetLib4Mirror;

public class CustomLiteNetLib4MirrorTransport : LiteNetLib4MirrorTransport
{
	private enum ClientType : byte
	{
		GameClient,
		VerificationService
	}

	private static readonly NetDataWriter RequestWriter;

	public static GeoblockingMode Geoblocking;

	public static ChallengeType ChallengeMode;

	public static ushort ChallengeInitLen;

	public static ushort ChallengeSecretLen;

	public static readonly Dictionary<IPEndPoint, PreauthItem> UserIds;

	public static readonly HashSet<string> UserIdFastReload;

	public static readonly Dictionary<string, PreauthChallengeItem> Challenges;

	public static bool UserRateLimiting;

	public static bool IpRateLimiting;

	public static bool UseGlobalBans;

	public static bool GeoblockIgnoreWhitelisted;

	public static bool UseChallenge;

	public static bool DisplayPreauthLogs;

	private static bool _delayConnections;

	public static bool SuppressRejections;

	public static bool SuppressIssued;

	public static uint Rejected;

	public static uint ChallengeIssued;

	public static byte DelayTime;

	internal static byte DelayVolume;

	internal static byte DelayVolumeThreshold;

	public static readonly HashSet<string> UserRateLimit;

	public static readonly HashSet<IPAddress> IpRateLimit;

	public static readonly HashSet<string> GeoblockingList;

	public static RejectionReason LastRejectionReason;

	public static string LastCustomReason;

	public static string VerificationChallenge;

	public static string VerificationResponse;

	public static long LastBanExpiration;

	public static bool IpPassthroughEnabled;

	public static HashSet<IPAddress> TrustedProxies;

	public static Dictionary<int, string> RealIpAddresses;

	public static uint RejectionThreshold;

	public static uint IssuedThreshold;

	public static bool DelayConnections
	{
		get
		{
			return CustomLiteNetLib4MirrorTransport._delayConnections;
		}
		set
		{
			if (CustomLiteNetLib4MirrorTransport._delayConnections != value)
			{
				if (!value)
				{
					CustomLiteNetLib4MirrorTransport.UserIds.Clear();
				}
				CustomLiteNetLib4MirrorTransport._delayConnections = value;
				ServerConsole.AddLog(value ? $"Incoming connections will be now delayed by {CustomLiteNetLib4MirrorTransport.DelayTime} seconds." : "Incoming connections will be no longer delayed.");
			}
		}
	}

	static CustomLiteNetLib4MirrorTransport()
	{
		CustomLiteNetLib4MirrorTransport.RequestWriter = new NetDataWriter();
		CustomLiteNetLib4MirrorTransport.Geoblocking = GeoblockingMode.None;
		CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.Reply;
		CustomLiteNetLib4MirrorTransport.UserIds = new Dictionary<IPEndPoint, PreauthItem>();
		CustomLiteNetLib4MirrorTransport.UserIdFastReload = new HashSet<string>(StringComparer.Ordinal);
		CustomLiteNetLib4MirrorTransport.Challenges = new Dictionary<string, PreauthChallengeItem>();
		CustomLiteNetLib4MirrorTransport._delayConnections = true;
		CustomLiteNetLib4MirrorTransport.DelayTime = 3;
		CustomLiteNetLib4MirrorTransport.UserRateLimit = new HashSet<string>(StringComparer.Ordinal);
		CustomLiteNetLib4MirrorTransport.IpRateLimit = new HashSet<IPAddress>();
		CustomLiteNetLib4MirrorTransport.GeoblockingList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CustomLiteNetLib4MirrorTransport.RejectionThreshold = 60u;
		CustomLiteNetLib4MirrorTransport.IssuedThreshold = 50u;
	}

	protected internal override void ProcessConnectionRequest(ConnectionRequest request)
	{
		try
		{
			if (!request.Data.TryGetByte(out var result) || result >= 2)
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)2);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			if (result == 1)
			{
				if (CustomLiteNetLib4MirrorTransport.VerificationChallenge != null && request.Data.TryGetString(out var result2) && result2 == CustomLiteNetLib4MirrorTransport.VerificationChallenge)
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)18);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.VerificationResponse);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.VerificationChallenge = null;
					CustomLiteNetLib4MirrorTransport.VerificationResponse = null;
					ServerConsole.AddLog("Verification challenge and response have been sent.\nThe system has successfully checked your server, a verification response will be printed to your console shortly, please allow up to 5 minutes.", ConsoleColor.Green);
					return;
				}
				CustomLiteNetLib4MirrorTransport.Rejected++;
				if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
				{
					CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
				}
				if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
				{
					ServerConsole.AddLog($"Invalid verification challenge has been received from endpoint {request.RemoteEndPoint}.");
				}
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)19);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			byte result3 = 0;
			if (!request.Data.TryGetByte(out var result4) || !request.Data.TryGetByte(out var result5) || !request.Data.TryGetByte(out var result6) || !request.Data.TryGetBool(out var result7) || (result7 && !request.Data.TryGetByte(out result3)))
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)3);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			if (!GameCore.Version.CompatibilityCheck(GameCore.Version.Major, GameCore.Version.Minor, GameCore.Version.Revision, result4, result5, result6, result7, result3))
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)3);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			int result8;
			bool flag = request.Data.TryGetInt(out result8);
			if (!request.Data.TryGetBytesWithLength(out var result9))
			{
				flag = false;
			}
			if (!flag)
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)15);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			if (CustomLiteNetLib4MirrorTransport.DelayConnections)
			{
				CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)17);
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.DelayTime);
				if (CustomLiteNetLib4MirrorTransport.DelayVolume < byte.MaxValue)
				{
					CustomLiteNetLib4MirrorTransport.DelayVolume++;
				}
				if (CustomLiteNetLib4MirrorTransport.DelayVolume < CustomLiteNetLib4MirrorTransport.DelayVolumeThreshold)
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Delayed connection incoming from endpoint {request.RemoteEndPoint} by {CustomLiteNetLib4MirrorTransport.DelayTime} seconds.");
					}
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
				}
				else
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Force delayed connection incoming from endpoint {request.RemoteEndPoint} by {CustomLiteNetLib4MirrorTransport.DelayTime} seconds.");
					}
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				}
				return;
			}
			if (CustomLiteNetLib4MirrorTransport.UseChallenge)
			{
				if (result8 == 0 || result9 == null || result9.Length == 0)
				{
					if (!CustomLiteNetLib4MirrorTransport.CheckIpRateLimit(request))
					{
						return;
					}
					int num = 0;
					string key = string.Empty;
					for (byte b = 0; b < 3; b++)
					{
						num = RandomGenerator.GetInt32();
						if (num == 0)
						{
							num = 1;
						}
						key = request.RemoteEndPoint.Address?.ToString() + "-" + num;
						if (!CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(key))
						{
							break;
						}
						if (b == 2)
						{
							CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
							CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)4);
							request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
							if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
							{
								ServerConsole.AddLog($"Failed to generate ID for challenge for incoming connection from endpoint {request.RemoteEndPoint}.");
							}
							return;
						}
					}
					byte[] bytes = RandomGenerator.GetBytes(CustomLiteNetLib4MirrorTransport.ChallengeInitLen + CustomLiteNetLib4MirrorTransport.ChallengeSecretLen, secure: true);
					CustomLiteNetLib4MirrorTransport.ChallengeIssued++;
					if (CustomLiteNetLib4MirrorTransport.ChallengeIssued > CustomLiteNetLib4MirrorTransport.IssuedThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressIssued = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressIssued && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Requested challenge for incoming connection from endpoint {request.RemoteEndPoint}.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)13);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)CustomLiteNetLib4MirrorTransport.ChallengeMode);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(num);
					switch (CustomLiteNetLib4MirrorTransport.ChallengeMode)
					{
					case ChallengeType.MD5:
						CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes, 0, CustomLiteNetLib4MirrorTransport.ChallengeInitLen);
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.ChallengeSecretLen);
						CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(Md.Md5(bytes));
						CustomLiteNetLib4MirrorTransport.Challenges.Add(key, new PreauthChallengeItem(new ArraySegment<byte>(bytes, CustomLiteNetLib4MirrorTransport.ChallengeInitLen, CustomLiteNetLib4MirrorTransport.ChallengeSecretLen)));
						break;
					case ChallengeType.SHA1:
						CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes, 0, CustomLiteNetLib4MirrorTransport.ChallengeInitLen);
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.ChallengeSecretLen);
						CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(Sha.Sha1(bytes));
						CustomLiteNetLib4MirrorTransport.Challenges.Add(key, new PreauthChallengeItem(new ArraySegment<byte>(bytes, CustomLiteNetLib4MirrorTransport.ChallengeInitLen, CustomLiteNetLib4MirrorTransport.ChallengeSecretLen)));
						break;
					default:
						CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes);
						CustomLiteNetLib4MirrorTransport.Challenges.Add(key, new PreauthChallengeItem(new ArraySegment<byte>(bytes)));
						break;
					}
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
					return;
				}
				string key2 = request.RemoteEndPoint.Address?.ToString() + "-" + result8;
				if (!CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(key2))
				{
					CustomLiteNetLib4MirrorTransport.Rejected++;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Security challenge response of incoming connection from endpoint {request.RemoteEndPoint} has been REJECTED (invalid Challenge ID).");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)14);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					return;
				}
				ArraySegment<byte> validResponse = CustomLiteNetLib4MirrorTransport.Challenges[key2].ValidResponse;
				if (!result9.SequenceEqual(validResponse))
				{
					CustomLiteNetLib4MirrorTransport.Rejected++;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Security challenge response of incoming connection from endpoint {request.RemoteEndPoint} has been REJECTED (invalid response).");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)15);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					return;
				}
				CustomLiteNetLib4MirrorTransport.Challenges.Remove(key2);
				CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
				if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
				{
					ServerConsole.AddLog($"Security challenge response of incoming connection from endpoint {request.RemoteEndPoint} has been accepted.");
				}
			}
			else if (!CustomLiteNetLib4MirrorTransport.CheckIpRateLimit(request))
			{
				return;
			}
			int position = request.Data.Position;
			if (!PlayerAuthenticationManager.OnlineMode)
			{
				KeyValuePair<BanDetails, BanDetails> keyValuePair = BanHandler.QueryBan(null, request.RemoteEndPoint.Address.ToString());
				if (keyValuePair.Value != null)
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog($"Player tried to connect from banned endpoint {request.RemoteEndPoint}.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)6);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(keyValuePair.Value.Expires);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(keyValuePair.Value?.Reason ?? string.Empty);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				PlayerPreAuthenticatingEventArgs e = new PlayerPreAuthenticatingEventArgs(!CustomLiteNetLib4MirrorTransport.IsServerFull(), string.Empty, request.RemoteEndPoint.Address.ToString(), 0L, CentralAuthPreauthFlags.None, string.Empty, null, request, position);
				PlayerEvents.OnPreAuthenticating(e);
				if (!e.IsAllowed)
				{
					if (e.CustomReject != null)
					{
						if (e.ForceReject)
						{
							request.RejectForce(e.CustomReject);
						}
						else
						{
							request.Reject(e.CustomReject);
						}
					}
					else
					{
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)4);
						request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					}
				}
				else if (e.CanJoin)
				{
					request.Accept();
					CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
					PlayerEvents.OnPreAuthenticated(new PlayerPreAuthenticatedEventArgs(string.Empty, request.RemoteEndPoint.Address.ToString(), 0L, CentralAuthPreauthFlags.None, string.Empty, null, request, position));
				}
				else
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)1);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
				}
				return;
			}
			if (!request.Data.TryGetString(out var result10) || result10 == string.Empty)
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)5);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			if (!request.Data.TryGetLong(out var result11) || !request.Data.TryGetByte(out var result12) || !request.Data.TryGetString(out var result13) || !request.Data.TryGetBytesWithLength(out var result14))
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)4);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				return;
			}
			string result15 = null;
			string text = ((CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled && CustomLiteNetLib4MirrorTransport.TrustedProxies.Contains(request.RemoteEndPoint.Address) && request.Data.TryGetString(out result15)) ? $"{result15} [routed via {request.RemoteEndPoint}]" : request.RemoteEndPoint.ToString());
			CentralAuthPreauthFlags flags = (CentralAuthPreauthFlags)result12;
			try
			{
				if (!ECDSA.VerifyBytes($"{result10};{result12};{result13};{result11}", result14, ServerConsole.PublicKey))
				{
					CustomLiteNetLib4MirrorTransport.Rejected++;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player from endpoint " + text + " sent preauthentication token with invalid digital signature.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)2);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if (TimeBehaviour.CurrentUnixTimestamp > result11)
				{
					CustomLiteNetLib4MirrorTransport.Rejected++;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player from endpoint " + text + " sent expired preauthentication token.");
						ServerConsole.AddLog("Make sure that time and timezone set on server is correct. We recommend synchronizing the time.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)11);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if (CustomLiteNetLib4MirrorTransport.UserRateLimiting)
				{
					if (CustomLiteNetLib4MirrorTransport.UserRateLimit.Contains(result10))
					{
						CustomLiteNetLib4MirrorTransport.Rejected++;
						if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
						{
							CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
						}
						if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
						{
							ServerConsole.AddLog("Incoming connection from " + result10 + " (" + text + ") rejected due to exceeding the rate limit.");
						}
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)12);
						request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
						CustomLiteNetLib4MirrorTransport.ResetIdleMode();
						return;
					}
					CustomLiteNetLib4MirrorTransport.UserRateLimit.Add(result10);
				}
				if (!flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreBans) || !CustomNetworkManager.IsVerified)
				{
					KeyValuePair<BanDetails, BanDetails> keyValuePair2 = BanHandler.QueryBan(result10, result15 ?? request.RemoteEndPoint.Address.ToString());
					if (keyValuePair2.Key != null || keyValuePair2.Value != null)
					{
						CustomLiteNetLib4MirrorTransport.Rejected++;
						if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
						{
							CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
						}
						if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
						{
							ServerConsole.AddLog(((keyValuePair2.Key == null) ? "Player" : "Banned player") + " " + result10 + " tried to connect from" + ((keyValuePair2.Value == null) ? "" : " banned") + " endpoint " + text + ".");
							ServerLogs.AddLog(ServerLogs.Modules.Networking, ((keyValuePair2.Key == null) ? "Player" : "Banned player") + " " + result10 + " tried to connect from" + ((keyValuePair2.Value == null) ? "" : " banned") + " endpoint " + text + ".", ServerLogs.ServerLogType.ConnectionUpdate);
						}
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)6);
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(keyValuePair2.Key?.Expires ?? keyValuePair2.Value.Expires);
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(keyValuePair2.Key?.Reason ?? keyValuePair2.Value?.Reason ?? string.Empty);
						request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
						CustomLiteNetLib4MirrorTransport.ResetIdleMode();
						return;
					}
				}
				if (flags.HasFlagFast(CentralAuthPreauthFlags.AuthRejected))
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player " + result10 + " (" + text + ") kicked due to auth rejection by central server.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)20);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if (flags.HasFlagFast(CentralAuthPreauthFlags.GloballyBanned) && (CustomNetworkManager.IsVerified || CustomLiteNetLib4MirrorTransport.UseGlobalBans))
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player " + result10 + " (" + text + ") kicked due to an active global ban.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)8);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if ((!flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist) || !CustomNetworkManager.IsVerified) && !WhiteList.IsWhitelisted(result10))
				{
					if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player " + result10 + " tried joined from endpoint " + text + ", but is not whitelisted.");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)7);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if (CustomLiteNetLib4MirrorTransport.Geoblocking != GeoblockingMode.None && (!flags.HasFlagFast(CentralAuthPreauthFlags.IgnoreGeoblock) || !ServerStatic.PermissionsHandler.BanTeamBypassGeo) && (!CustomLiteNetLib4MirrorTransport.GeoblockIgnoreWhitelisted || !WhiteList.IsOnWhitelist(result10)) && ((CustomLiteNetLib4MirrorTransport.Geoblocking == GeoblockingMode.Whitelist && !CustomLiteNetLib4MirrorTransport.GeoblockingList.Contains(result13)) || (CustomLiteNetLib4MirrorTransport.Geoblocking == GeoblockingMode.Blacklist && CustomLiteNetLib4MirrorTransport.GeoblockingList.Contains(result13))))
				{
					CustomLiteNetLib4MirrorTransport.Rejected++;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog("Player " + result10 + " (" + text + ") tried joined from blocked country " + result13 + ".");
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)9);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
					return;
				}
				if (CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(result10))
				{
					CustomLiteNetLib4MirrorTransport.UserIdFastReload.Remove(result10);
				}
				PlayerPreAuthenticatingEventArgs e2 = new PlayerPreAuthenticatingEventArgs(!CustomLiteNetLib4MirrorTransport.IsServerFull(result10, flags), result10, (result15 == null) ? request.RemoteEndPoint.Address.ToString() : result15, result11, flags, result13, result14, request, position);
				PlayerEvents.OnPreAuthenticating(e2);
				if (!e2.IsAllowed)
				{
					if (e2.CustomReject != null)
					{
						if (e2.ForceReject)
						{
							request.RejectForce(e2.CustomReject);
						}
						else
						{
							request.Reject(e2.CustomReject);
						}
					}
					else
					{
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)4);
						request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					}
				}
				else if (e2.CanJoin)
				{
					if (CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(request.RemoteEndPoint))
					{
						CustomLiteNetLib4MirrorTransport.UserIds[request.RemoteEndPoint].SetUserId(result10);
					}
					else
					{
						CustomLiteNetLib4MirrorTransport.UserIds.Add(request.RemoteEndPoint, new PreauthItem(result10));
					}
					NetPeer netPeer = request.Accept();
					if (result15 != null)
					{
						if (CustomLiteNetLib4MirrorTransport.RealIpAddresses.ContainsKey(netPeer.Id))
						{
							CustomLiteNetLib4MirrorTransport.RealIpAddresses[netPeer.Id] = result15;
						}
						else
						{
							CustomLiteNetLib4MirrorTransport.RealIpAddresses.Add(netPeer.Id, result15);
						}
					}
					ServerConsole.AddLog("Player " + result10 + " preauthenticated from endpoint " + text + ".");
					ServerLogs.AddLog(ServerLogs.Modules.Networking, result10 + " preauthenticated from endpoint " + text + ".", ServerLogs.ServerLogType.ConnectionUpdate);
					CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
					PlayerEvents.OnPreAuthenticated(new PlayerPreAuthenticatedEventArgs(result10, (result15 == null) ? request.RemoteEndPoint.Address.ToString() : result15, result11, flags, result13, result14, request, position));
				}
				else
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)1);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.ResetIdleMode();
				}
			}
			catch (Exception ex)
			{
				CustomLiteNetLib4MirrorTransport.Rejected++;
				if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
				{
					CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
				}
				if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
				{
					ServerConsole.AddLog("Player from endpoint " + text + " sent an invalid preauthentication token. " + ex.Message);
				}
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)2);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				CustomLiteNetLib4MirrorTransport.ResetIdleMode();
			}
		}
		catch (Exception ex2)
		{
			CustomLiteNetLib4MirrorTransport.Rejected++;
			if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
			{
				CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
			}
			if (!CustomLiteNetLib4MirrorTransport.SuppressRejections)
			{
				ServerConsole.AddLog($"Player from endpoint {request.RemoteEndPoint} failed to preauthenticate: {ex2.Message}");
			}
			CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
			CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)4);
			request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
		}
	}

	private static bool IsServerFull(string userId = null, CentralAuthPreauthFlags flags = CentralAuthPreauthFlags.None)
	{
		if (!string.IsNullOrEmpty(userId) && CustomLiteNetLib4MirrorTransport.HasReservedSlot(userId, flags))
		{
			return false;
		}
		return LiteNetLib4MirrorCore.Host.ConnectedPeersCount >= CustomNetworkManager.slots;
	}

	private static bool HasReservedSlot(string userId, CentralAuthPreauthFlags flags)
	{
		if (flags.HasFlagFast(CentralAuthPreauthFlags.ReservedSlot) && ServerStatic.PermissionsHandler.BanTeamSlots)
		{
			return true;
		}
		if (!ConfigFile.ServerConfig.GetBool("use_reserved_slots", def: true))
		{
			return false;
		}
		if (!ReservedSlot.HasReservedSlot(userId))
		{
			return false;
		}
		return CustomNetworkManager.slots + CustomNetworkManager.reservedSlots - LiteNetLib4MirrorCore.Host.ConnectedPeersCount > 0;
	}

	private static bool CheckIpRateLimit(ConnectionRequest request)
	{
		if (!CustomLiteNetLib4MirrorTransport.IpRateLimiting)
		{
			return true;
		}
		if (CustomLiteNetLib4MirrorTransport.IpRateLimit.Contains(request.RemoteEndPoint.Address))
		{
			CustomLiteNetLib4MirrorTransport.Rejected++;
			if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
			{
				CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
			}
			if (!CustomLiteNetLib4MirrorTransport.SuppressRejections)
			{
				ServerConsole.AddLog($"Incoming connection from endpoint {request.RemoteEndPoint} rejected due to exceeding the rate limit.");
				ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Incoming connection from endpoint {request.RemoteEndPoint} rejected due to exceeding the rate limit.", ServerLogs.ServerLogType.AuthRateLimit);
			}
			CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
			CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)12);
			request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
			return false;
		}
		CustomLiteNetLib4MirrorTransport.IpRateLimit.Add(request.RemoteEndPoint.Address);
		return true;
	}

	protected override void GetConnectData(NetDataWriter writer)
	{
	}

	protected internal override void OnConncetionRefused(DisconnectInfo disconnectinfo)
	{
		if (disconnectinfo.AdditionalData.TryGetByte(out var result))
		{
			CustomLiteNetLib4MirrorTransport.LastRejectionReason = (RejectionReason)result;
		}
		else
		{
			CustomLiteNetLib4MirrorTransport.LastRejectionReason = RejectionReason.NotSpecified;
		}
	}

	private static void ResetIdleMode()
	{
		if (LiteNetLib4MirrorCore.Host.ConnectedPeersCount == 0)
		{
			IdleMode.SetIdleMode(state: true);
		}
	}

	public static void PreauthDisableIdleMode()
	{
		if (ServerStatic.IsDedicated && IdleMode.IdleModeActive)
		{
			IdleMode.PreauthStopwatch.Restart();
			IdleMode.SetIdleMode(state: false);
		}
	}

	public static void ReloadChallengeOptions()
	{
		CustomLiteNetLib4MirrorTransport.UseChallenge = ConfigFile.ServerConfig.GetBool("preauth_challenge", def: true);
		CustomLiteNetLib4MirrorTransport.ChallengeInitLen = ConfigFile.ServerConfig.GetUShort("preauth_challenge_base_length", 16);
		CustomLiteNetLib4MirrorTransport.ChallengeSecretLen = ConfigFile.ServerConfig.GetUShort("preauth_challenge_secret_length", 5);
		string text = ConfigFile.ServerConfig.GetString("preauth_challenge_mode", "reply").ToLower();
		if (!(text == "md5"))
		{
			if (text == "sha1")
			{
				CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.SHA1;
				return;
			}
			CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.Reply;
			CustomLiteNetLib4MirrorTransport.ChallengeSecretLen = 0;
		}
		else
		{
			CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.MD5;
		}
	}
}
