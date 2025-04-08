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
	public static bool DelayConnections
	{
		get
		{
			return CustomLiteNetLib4MirrorTransport._delayConnections;
		}
		set
		{
			if (CustomLiteNetLib4MirrorTransport._delayConnections == value)
			{
				return;
			}
			if (!value)
			{
				CustomLiteNetLib4MirrorTransport.UserIds.Clear();
			}
			CustomLiteNetLib4MirrorTransport._delayConnections = value;
			ServerConsole.AddLog(value ? string.Format("Incoming connections will be now delayed by {0} seconds.", CustomLiteNetLib4MirrorTransport.DelayTime) : "Incoming connections will be no longer delayed.", ConsoleColor.Gray, false);
		}
	}

	protected internal override void ProcessConnectionRequest(ConnectionRequest request)
	{
		try
		{
			byte b;
			if (!request.Data.TryGetByte(out b) || b >= 2)
			{
				CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
				CustomLiteNetLib4MirrorTransport.RequestWriter.Put(2);
				request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
			}
			else if (b == 1)
			{
				string text;
				if (CustomLiteNetLib4MirrorTransport.VerificationChallenge != null && request.Data.TryGetString(out text) && text == CustomLiteNetLib4MirrorTransport.VerificationChallenge)
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(18);
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.VerificationResponse);
					request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
					CustomLiteNetLib4MirrorTransport.VerificationChallenge = null;
					CustomLiteNetLib4MirrorTransport.VerificationResponse = null;
					ServerConsole.AddLog("Verification challenge and response have been sent.\nThe system has successfully checked your server, a verification response will be printed to your console shortly, please allow up to 5 minutes.", ConsoleColor.Green, false);
				}
				else
				{
					CustomLiteNetLib4MirrorTransport.Rejected += 1U;
					if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
					{
						CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
					}
					if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
					{
						ServerConsole.AddLog(string.Format("Invalid verification challenge has been received from endpoint {0}.", request.RemoteEndPoint), ConsoleColor.Gray, false);
					}
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(19);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				}
			}
			else
			{
				byte b2 = 0;
				byte b3;
				byte b4;
				byte b5;
				bool flag;
				if (!request.Data.TryGetByte(out b3) || !request.Data.TryGetByte(out b4) || !request.Data.TryGetByte(out b5) || !request.Data.TryGetBool(out flag) || (flag && !request.Data.TryGetByte(out b2)))
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(3);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				}
				else if (!global::GameCore.Version.CompatibilityCheck(global::GameCore.Version.Major, global::GameCore.Version.Minor, global::GameCore.Version.Revision, b3, b4, b5, flag, b2))
				{
					CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
					CustomLiteNetLib4MirrorTransport.RequestWriter.Put(3);
					request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
				}
				else
				{
					int num;
					bool flag2 = request.Data.TryGetInt(out num);
					byte[] array;
					if (!request.Data.TryGetBytesWithLength(out array))
					{
						flag2 = false;
					}
					if (!flag2)
					{
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(15);
						request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
					}
					else if (CustomLiteNetLib4MirrorTransport.DelayConnections)
					{
						CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(17);
						CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.DelayTime);
						if (CustomLiteNetLib4MirrorTransport.DelayVolume < 255)
						{
							CustomLiteNetLib4MirrorTransport.DelayVolume += 1;
						}
						if (CustomLiteNetLib4MirrorTransport.DelayVolume < CustomLiteNetLib4MirrorTransport.DelayVolumeThreshold)
						{
							if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
							{
								ServerConsole.AddLog(string.Format("Delayed connection incoming from endpoint {0} by {1} seconds.", request.RemoteEndPoint, CustomLiteNetLib4MirrorTransport.DelayTime), ConsoleColor.Gray, false);
							}
							request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
						}
						else
						{
							if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
							{
								ServerConsole.AddLog(string.Format("Force delayed connection incoming from endpoint {0} by {1} seconds.", request.RemoteEndPoint, CustomLiteNetLib4MirrorTransport.DelayTime), ConsoleColor.Gray, false);
							}
							request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
						}
					}
					else
					{
						if (CustomLiteNetLib4MirrorTransport.UseChallenge)
						{
							if (num == 0 || array == null || array.Length == 0)
							{
								if (!CustomLiteNetLib4MirrorTransport.CheckIpRateLimit(request))
								{
									return;
								}
								int num2 = 0;
								string text2 = string.Empty;
								for (byte b6 = 0; b6 < 3; b6 += 1)
								{
									num2 = RandomGenerator.GetInt32(false);
									if (num2 == 0)
									{
										num2 = 1;
									}
									IPAddress address = request.RemoteEndPoint.Address;
									text2 = ((address != null) ? address.ToString() : null) + "-" + num2.ToString();
									if (!CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(text2))
									{
										break;
									}
									if (b6 == 2)
									{
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(4);
										request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
										if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
										{
											ServerConsole.AddLog(string.Format("Failed to generate ID for challenge for incoming connection from endpoint {0}.", request.RemoteEndPoint), ConsoleColor.Gray, false);
										}
										return;
									}
								}
								byte[] bytes = RandomGenerator.GetBytes((int)(CustomLiteNetLib4MirrorTransport.ChallengeInitLen + CustomLiteNetLib4MirrorTransport.ChallengeSecretLen), true);
								CustomLiteNetLib4MirrorTransport.ChallengeIssued += 1U;
								if (CustomLiteNetLib4MirrorTransport.ChallengeIssued > CustomLiteNetLib4MirrorTransport.IssuedThreshold)
								{
									CustomLiteNetLib4MirrorTransport.SuppressIssued = true;
								}
								if (!CustomLiteNetLib4MirrorTransport.SuppressIssued && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
								{
									ServerConsole.AddLog(string.Format("Requested challenge for incoming connection from endpoint {0}.", request.RemoteEndPoint), ConsoleColor.Gray, false);
								}
								CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put(13);
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put((byte)CustomLiteNetLib4MirrorTransport.ChallengeMode);
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put(num2);
								ChallengeType challengeMode = CustomLiteNetLib4MirrorTransport.ChallengeMode;
								if (challengeMode != ChallengeType.MD5)
								{
									if (challengeMode != ChallengeType.SHA1)
									{
										CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes);
										CustomLiteNetLib4MirrorTransport.Challenges.Add(text2, new PreauthChallengeItem(new ArraySegment<byte>(bytes)));
									}
									else
									{
										CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes, 0, CustomLiteNetLib4MirrorTransport.ChallengeInitLen);
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.ChallengeSecretLen);
										CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(Sha.Sha1(bytes));
										CustomLiteNetLib4MirrorTransport.Challenges.Add(text2, new PreauthChallengeItem(new ArraySegment<byte>(bytes, (int)CustomLiteNetLib4MirrorTransport.ChallengeInitLen, (int)CustomLiteNetLib4MirrorTransport.ChallengeSecretLen)));
									}
								}
								else
								{
									CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(bytes, 0, CustomLiteNetLib4MirrorTransport.ChallengeInitLen);
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(CustomLiteNetLib4MirrorTransport.ChallengeSecretLen);
									CustomLiteNetLib4MirrorTransport.RequestWriter.PutBytesWithLength(Md.Md5(bytes));
									CustomLiteNetLib4MirrorTransport.Challenges.Add(text2, new PreauthChallengeItem(new ArraySegment<byte>(bytes, (int)CustomLiteNetLib4MirrorTransport.ChallengeInitLen, (int)CustomLiteNetLib4MirrorTransport.ChallengeSecretLen)));
								}
								request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
								CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
								return;
							}
							else
							{
								IPAddress address2 = request.RemoteEndPoint.Address;
								string text3 = ((address2 != null) ? address2.ToString() : null) + "-" + num.ToString();
								if (!CustomLiteNetLib4MirrorTransport.Challenges.ContainsKey(text3))
								{
									CustomLiteNetLib4MirrorTransport.Rejected += 1U;
									if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
									{
										CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
									}
									if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
									{
										ServerConsole.AddLog(string.Format("Security challenge response of incoming connection from endpoint {0} has been REJECTED (invalid Challenge ID).", request.RemoteEndPoint), ConsoleColor.Gray, false);
									}
									CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(14);
									request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
									return;
								}
								ArraySegment<byte> validResponse = CustomLiteNetLib4MirrorTransport.Challenges[text3].ValidResponse;
								if (!array.SequenceEqual(validResponse))
								{
									CustomLiteNetLib4MirrorTransport.Rejected += 1U;
									if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
									{
										CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
									}
									if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
									{
										ServerConsole.AddLog(string.Format("Security challenge response of incoming connection from endpoint {0} has been REJECTED (invalid response).", request.RemoteEndPoint), ConsoleColor.Gray, false);
									}
									CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(15);
									request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
									return;
								}
								CustomLiteNetLib4MirrorTransport.Challenges.Remove(text3);
								CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
								if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
								{
									ServerConsole.AddLog(string.Format("Security challenge response of incoming connection from endpoint {0} has been accepted.", request.RemoteEndPoint), ConsoleColor.Gray, false);
								}
							}
						}
						else if (!CustomLiteNetLib4MirrorTransport.CheckIpRateLimit(request))
						{
							return;
						}
						int position = request.Data.Position;
						string text4;
						long num3;
						byte b7;
						string text5;
						byte[] array2;
						if (!PlayerAuthenticationManager.OnlineMode)
						{
							KeyValuePair<BanDetails, BanDetails> keyValuePair = BanHandler.QueryBan(null, request.RemoteEndPoint.Address.ToString());
							if (keyValuePair.Value != null)
							{
								if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
								{
									ServerConsole.AddLog(string.Format("Player tried to connect from banned endpoint {0}.", request.RemoteEndPoint), ConsoleColor.Gray, false);
								}
								CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put(6);
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put(keyValuePair.Value.Expires);
								NetDataWriter requestWriter = CustomLiteNetLib4MirrorTransport.RequestWriter;
								BanDetails value = keyValuePair.Value;
								requestWriter.Put(((value != null) ? value.Reason : null) ?? string.Empty);
								request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
								CustomLiteNetLib4MirrorTransport.ResetIdleMode();
							}
							else
							{
								PlayerPreAuthenticatingEventArgs playerPreAuthenticatingEventArgs = new PlayerPreAuthenticatingEventArgs(!CustomLiteNetLib4MirrorTransport.IsServerFull(null, CentralAuthPreauthFlags.None), string.Empty, request.RemoteEndPoint.Address.ToString(), 0L, CentralAuthPreauthFlags.None, string.Empty, null, request, position);
								PlayerEvents.OnPreAuthenticating(playerPreAuthenticatingEventArgs);
								if (!playerPreAuthenticatingEventArgs.IsAllowed)
								{
									if (playerPreAuthenticatingEventArgs.CustomReject != null)
									{
										if (playerPreAuthenticatingEventArgs.ForceReject)
										{
											request.RejectForce(playerPreAuthenticatingEventArgs.CustomReject);
										}
										else
										{
											request.Reject(playerPreAuthenticatingEventArgs.CustomReject);
										}
									}
									else
									{
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(4);
										request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
									}
								}
								else if (playerPreAuthenticatingEventArgs.CanJoin)
								{
									request.Accept();
									CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
									PlayerEvents.OnPreAuthenticated(new PlayerPreAuthenticatedEventArgs(string.Empty, request.RemoteEndPoint.Address.ToString(), 0L, CentralAuthPreauthFlags.None, string.Empty, null, request, position));
								}
								else
								{
									CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(1);
									request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
									CustomLiteNetLib4MirrorTransport.ResetIdleMode();
								}
							}
						}
						else if (!request.Data.TryGetString(out text4) || text4 == string.Empty)
						{
							CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
							CustomLiteNetLib4MirrorTransport.RequestWriter.Put(5);
							request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
						}
						else if (!request.Data.TryGetLong(out num3) || !request.Data.TryGetByte(out b7) || !request.Data.TryGetString(out text5) || !request.Data.TryGetBytesWithLength(out array2))
						{
							CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
							CustomLiteNetLib4MirrorTransport.RequestWriter.Put(4);
							request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
						}
						else
						{
							string text6 = null;
							string text7;
							if (!CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled || !CustomLiteNetLib4MirrorTransport.TrustedProxies.Contains(request.RemoteEndPoint.Address) || !request.Data.TryGetString(out text6))
							{
								text7 = request.RemoteEndPoint.ToString();
							}
							else
							{
								text7 = string.Format("{0} [routed via {1}]", text6, request.RemoteEndPoint);
							}
							CentralAuthPreauthFlags centralAuthPreauthFlags = (CentralAuthPreauthFlags)b7;
							try
							{
								if (!ECDSA.VerifyBytes(string.Format("{0};{1};{2};{3}", new object[] { text4, b7, text5, num3 }), array2, ServerConsole.PublicKey))
								{
									CustomLiteNetLib4MirrorTransport.Rejected += 1U;
									if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
									{
										CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
									}
									if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
									{
										ServerConsole.AddLog("Player from endpoint " + text7 + " sent preauthentication token with invalid digital signature.", ConsoleColor.Gray, false);
									}
									CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(2);
									request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
									CustomLiteNetLib4MirrorTransport.ResetIdleMode();
								}
								else if (TimeBehaviour.CurrentUnixTimestamp > num3)
								{
									CustomLiteNetLib4MirrorTransport.Rejected += 1U;
									if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
									{
										CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
									}
									if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
									{
										ServerConsole.AddLog("Player from endpoint " + text7 + " sent expired preauthentication token.", ConsoleColor.Gray, false);
										ServerConsole.AddLog("Make sure that time and timezone set on server is correct. We recommend synchronizing the time.", ConsoleColor.Gray, false);
									}
									CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
									CustomLiteNetLib4MirrorTransport.RequestWriter.Put(11);
									request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
									CustomLiteNetLib4MirrorTransport.ResetIdleMode();
								}
								else
								{
									if (CustomLiteNetLib4MirrorTransport.UserRateLimiting)
									{
										if (CustomLiteNetLib4MirrorTransport.UserRateLimit.Contains(text4))
										{
											CustomLiteNetLib4MirrorTransport.Rejected += 1U;
											if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
											{
												CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
											}
											if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
											{
												ServerConsole.AddLog(string.Concat(new string[] { "Incoming connection from ", text4, " (", text7, ") rejected due to exceeding the rate limit." }), ConsoleColor.Gray, false);
											}
											CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
											CustomLiteNetLib4MirrorTransport.RequestWriter.Put(12);
											request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
											CustomLiteNetLib4MirrorTransport.ResetIdleMode();
											return;
										}
										CustomLiteNetLib4MirrorTransport.UserRateLimit.Add(text4);
									}
									if (!centralAuthPreauthFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreBans) || !CustomNetworkManager.IsVerified)
									{
										KeyValuePair<BanDetails, BanDetails> keyValuePair2 = BanHandler.QueryBan(text4, text6 ?? request.RemoteEndPoint.Address.ToString());
										if (keyValuePair2.Key != null || keyValuePair2.Value != null)
										{
											CustomLiteNetLib4MirrorTransport.Rejected += 1U;
											if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
											{
												CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
											}
											if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
											{
												ServerConsole.AddLog(string.Concat(new string[]
												{
													(keyValuePair2.Key == null) ? "Player" : "Banned player",
													" ",
													text4,
													" tried to connect from",
													(keyValuePair2.Value == null) ? "" : " banned",
													" endpoint ",
													text7,
													"."
												}), ConsoleColor.Gray, false);
												ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Concat(new string[]
												{
													(keyValuePair2.Key == null) ? "Player" : "Banned player",
													" ",
													text4,
													" tried to connect from",
													(keyValuePair2.Value == null) ? "" : " banned",
													" endpoint ",
													text7,
													"."
												}), ServerLogs.ServerLogType.ConnectionUpdate, false);
											}
											CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
											CustomLiteNetLib4MirrorTransport.RequestWriter.Put(6);
											NetDataWriter requestWriter2 = CustomLiteNetLib4MirrorTransport.RequestWriter;
											BanDetails key = keyValuePair2.Key;
											requestWriter2.Put((key != null) ? key.Expires : keyValuePair2.Value.Expires);
											NetDataWriter requestWriter3 = CustomLiteNetLib4MirrorTransport.RequestWriter;
											BanDetails key2 = keyValuePair2.Key;
											string text8;
											if ((text8 = ((key2 != null) ? key2.Reason : null)) == null)
											{
												BanDetails value2 = keyValuePair2.Value;
												text8 = ((value2 != null) ? value2.Reason : null) ?? string.Empty;
											}
											requestWriter3.Put(text8);
											request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
											CustomLiteNetLib4MirrorTransport.ResetIdleMode();
											return;
										}
									}
									if (centralAuthPreauthFlags.HasFlagFast(CentralAuthPreauthFlags.AuthRejected))
									{
										if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
										{
											ServerConsole.AddLog(string.Concat(new string[] { "Player ", text4, " (", text7, ") kicked due to auth rejection by central server." }), ConsoleColor.Gray, false);
										}
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(20);
										request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
										CustomLiteNetLib4MirrorTransport.ResetIdleMode();
									}
									else if (centralAuthPreauthFlags.HasFlagFast(CentralAuthPreauthFlags.GloballyBanned) && (CustomNetworkManager.IsVerified || CustomLiteNetLib4MirrorTransport.UseGlobalBans))
									{
										if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
										{
											ServerConsole.AddLog(string.Concat(new string[] { "Player ", text4, " (", text7, ") kicked due to an active global ban." }), ConsoleColor.Gray, false);
										}
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(8);
										request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
										CustomLiteNetLib4MirrorTransport.ResetIdleMode();
									}
									else if ((!centralAuthPreauthFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist) || !CustomNetworkManager.IsVerified) && !WhiteList.IsWhitelisted(text4))
									{
										if (CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
										{
											ServerConsole.AddLog(string.Concat(new string[] { "Player ", text4, " tried joined from endpoint ", text7, ", but is not whitelisted." }), ConsoleColor.Gray, false);
										}
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(7);
										request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
										CustomLiteNetLib4MirrorTransport.ResetIdleMode();
									}
									else if (CustomLiteNetLib4MirrorTransport.Geoblocking != GeoblockingMode.None && (!centralAuthPreauthFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreGeoblock) || !ServerStatic.PermissionsHandler.BanTeamBypassGeo) && (!CustomLiteNetLib4MirrorTransport.GeoblockIgnoreWhitelisted || !WhiteList.IsOnWhitelist(text4)) && ((CustomLiteNetLib4MirrorTransport.Geoblocking == GeoblockingMode.Whitelist && !CustomLiteNetLib4MirrorTransport.GeoblockingList.Contains(text5)) || (CustomLiteNetLib4MirrorTransport.Geoblocking == GeoblockingMode.Blacklist && CustomLiteNetLib4MirrorTransport.GeoblockingList.Contains(text5))))
									{
										CustomLiteNetLib4MirrorTransport.Rejected += 1U;
										if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
										{
											CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
										}
										if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
										{
											ServerConsole.AddLog(string.Concat(new string[] { "Player ", text4, " (", text7, ") tried joined from blocked country ", text5, "." }), ConsoleColor.Gray, false);
										}
										CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
										CustomLiteNetLib4MirrorTransport.RequestWriter.Put(9);
										request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
										CustomLiteNetLib4MirrorTransport.ResetIdleMode();
									}
									else
									{
										if (CustomLiteNetLib4MirrorTransport.UserIdFastReload.Contains(text4))
										{
											CustomLiteNetLib4MirrorTransport.UserIdFastReload.Remove(text4);
										}
										PlayerPreAuthenticatingEventArgs playerPreAuthenticatingEventArgs2 = new PlayerPreAuthenticatingEventArgs(!CustomLiteNetLib4MirrorTransport.IsServerFull(text4, centralAuthPreauthFlags), text4, (text6 == null) ? request.RemoteEndPoint.Address.ToString() : text6, num3, centralAuthPreauthFlags, text5, array2, request, position);
										PlayerEvents.OnPreAuthenticating(playerPreAuthenticatingEventArgs2);
										if (!playerPreAuthenticatingEventArgs2.IsAllowed)
										{
											if (playerPreAuthenticatingEventArgs2.CustomReject != null)
											{
												if (playerPreAuthenticatingEventArgs2.ForceReject)
												{
													request.RejectForce(playerPreAuthenticatingEventArgs2.CustomReject);
												}
												else
												{
													request.Reject(playerPreAuthenticatingEventArgs2.CustomReject);
												}
											}
											else
											{
												CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
												CustomLiteNetLib4MirrorTransport.RequestWriter.Put(4);
												request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
											}
										}
										else if (playerPreAuthenticatingEventArgs2.CanJoin)
										{
											if (CustomLiteNetLib4MirrorTransport.UserIds.ContainsKey(request.RemoteEndPoint))
											{
												CustomLiteNetLib4MirrorTransport.UserIds[request.RemoteEndPoint].SetUserId(text4);
											}
											else
											{
												CustomLiteNetLib4MirrorTransport.UserIds.Add(request.RemoteEndPoint, new PreauthItem(text4));
											}
											NetPeer netPeer = request.Accept();
											if (text6 != null)
											{
												if (CustomLiteNetLib4MirrorTransport.RealIpAddresses.ContainsKey(netPeer.Id))
												{
													CustomLiteNetLib4MirrorTransport.RealIpAddresses[netPeer.Id] = text6;
												}
												else
												{
													CustomLiteNetLib4MirrorTransport.RealIpAddresses.Add(netPeer.Id, text6);
												}
											}
											ServerConsole.AddLog(string.Concat(new string[] { "Player ", text4, " preauthenticated from endpoint ", text7, "." }), ConsoleColor.Gray, false);
											ServerLogs.AddLog(ServerLogs.Modules.Networking, text4 + " preauthenticated from endpoint " + text7 + ".", ServerLogs.ServerLogType.ConnectionUpdate, false);
											CustomLiteNetLib4MirrorTransport.PreauthDisableIdleMode();
											PlayerEvents.OnPreAuthenticated(new PlayerPreAuthenticatedEventArgs(text4, (text6 == null) ? request.RemoteEndPoint.Address.ToString() : text6, num3, centralAuthPreauthFlags, text5, array2, request, position));
										}
										else
										{
											CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
											CustomLiteNetLib4MirrorTransport.RequestWriter.Put(1);
											request.Reject(CustomLiteNetLib4MirrorTransport.RequestWriter);
											CustomLiteNetLib4MirrorTransport.ResetIdleMode();
										}
									}
								}
							}
							catch (Exception ex)
							{
								CustomLiteNetLib4MirrorTransport.Rejected += 1U;
								if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
								{
									CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
								}
								if (!CustomLiteNetLib4MirrorTransport.SuppressRejections && CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs)
								{
									ServerConsole.AddLog("Player from endpoint " + text7 + " sent an invalid preauthentication token. " + ex.Message, ConsoleColor.Gray, false);
								}
								CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
								CustomLiteNetLib4MirrorTransport.RequestWriter.Put(2);
								request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
								CustomLiteNetLib4MirrorTransport.ResetIdleMode();
							}
						}
					}
				}
			}
		}
		catch (Exception ex2)
		{
			CustomLiteNetLib4MirrorTransport.Rejected += 1U;
			if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
			{
				CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
			}
			if (!CustomLiteNetLib4MirrorTransport.SuppressRejections)
			{
				ServerConsole.AddLog(string.Format("Player from endpoint {0} failed to preauthenticate: {1}", request.RemoteEndPoint, ex2.Message), ConsoleColor.Gray, false);
			}
			CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
			CustomLiteNetLib4MirrorTransport.RequestWriter.Put(4);
			request.RejectForce(CustomLiteNetLib4MirrorTransport.RequestWriter);
		}
	}

	private static bool IsServerFull(string userId = null, CentralAuthPreauthFlags flags = CentralAuthPreauthFlags.None)
	{
		return (string.IsNullOrEmpty(userId) || !CustomLiteNetLib4MirrorTransport.HasReservedSlot(userId, flags)) && LiteNetLib4MirrorCore.Host.ConnectedPeersCount >= CustomNetworkManager.slots;
	}

	private static bool HasReservedSlot(string userId, CentralAuthPreauthFlags flags)
	{
		return (flags.HasFlagFast(CentralAuthPreauthFlags.ReservedSlot) && ServerStatic.PermissionsHandler.BanTeamSlots) || (ConfigFile.ServerConfig.GetBool("use_reserved_slots", true) && ReservedSlot.HasReservedSlot(userId) && CustomNetworkManager.slots + CustomNetworkManager.reservedSlots - LiteNetLib4MirrorCore.Host.ConnectedPeersCount > 0);
	}

	private static bool CheckIpRateLimit(ConnectionRequest request)
	{
		if (!CustomLiteNetLib4MirrorTransport.IpRateLimiting)
		{
			return true;
		}
		if (CustomLiteNetLib4MirrorTransport.IpRateLimit.Contains(request.RemoteEndPoint.Address))
		{
			CustomLiteNetLib4MirrorTransport.Rejected += 1U;
			if (CustomLiteNetLib4MirrorTransport.Rejected > CustomLiteNetLib4MirrorTransport.RejectionThreshold)
			{
				CustomLiteNetLib4MirrorTransport.SuppressRejections = true;
			}
			if (!CustomLiteNetLib4MirrorTransport.SuppressRejections)
			{
				ServerConsole.AddLog(string.Format("Incoming connection from endpoint {0} rejected due to exceeding the rate limit.", request.RemoteEndPoint), ConsoleColor.Gray, false);
				ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Incoming connection from endpoint {0} rejected due to exceeding the rate limit.", request.RemoteEndPoint), ServerLogs.ServerLogType.AuthRateLimit, false);
			}
			CustomLiteNetLib4MirrorTransport.RequestWriter.Reset();
			CustomLiteNetLib4MirrorTransport.RequestWriter.Put(12);
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
		byte b;
		if (disconnectinfo.AdditionalData.TryGetByte(out b))
		{
			CustomLiteNetLib4MirrorTransport.LastRejectionReason = (RejectionReason)b;
			return;
		}
		CustomLiteNetLib4MirrorTransport.LastRejectionReason = RejectionReason.NotSpecified;
	}

	private static void ResetIdleMode()
	{
		if (LiteNetLib4MirrorCore.Host.ConnectedPeersCount != 0)
		{
			return;
		}
		IdleMode.SetIdleMode(true);
	}

	public static void PreauthDisableIdleMode()
	{
		if (!ServerStatic.IsDedicated || !IdleMode.IdleModeActive)
		{
			return;
		}
		IdleMode.PreauthStopwatch.Restart();
		IdleMode.SetIdleMode(false);
	}

	public static void ReloadChallengeOptions()
	{
		CustomLiteNetLib4MirrorTransport.UseChallenge = ConfigFile.ServerConfig.GetBool("preauth_challenge", true);
		CustomLiteNetLib4MirrorTransport.ChallengeInitLen = ConfigFile.ServerConfig.GetUShort("preauth_challenge_base_length", 16);
		CustomLiteNetLib4MirrorTransport.ChallengeSecretLen = ConfigFile.ServerConfig.GetUShort("preauth_challenge_secret_length", 5);
		string text = ConfigFile.ServerConfig.GetString("preauth_challenge_mode", "reply").ToLower();
		if (text == "md5")
		{
			CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.MD5;
			return;
		}
		if (!(text == "sha1"))
		{
			CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.Reply;
			CustomLiteNetLib4MirrorTransport.ChallengeSecretLen = 0;
			return;
		}
		CustomLiteNetLib4MirrorTransport.ChallengeMode = ChallengeType.SHA1;
	}

	private static readonly NetDataWriter RequestWriter = new NetDataWriter();

	public static GeoblockingMode Geoblocking = GeoblockingMode.None;

	public static ChallengeType ChallengeMode = ChallengeType.Reply;

	public static ushort ChallengeInitLen;

	public static ushort ChallengeSecretLen;

	public static readonly Dictionary<IPEndPoint, PreauthItem> UserIds = new Dictionary<IPEndPoint, PreauthItem>();

	public static readonly HashSet<string> UserIdFastReload = new HashSet<string>(StringComparer.Ordinal);

	public static readonly Dictionary<string, PreauthChallengeItem> Challenges = new Dictionary<string, PreauthChallengeItem>();

	public static bool UserRateLimiting;

	public static bool IpRateLimiting;

	public static bool UseGlobalBans;

	public static bool GeoblockIgnoreWhitelisted;

	public static bool UseChallenge;

	public static bool DisplayPreauthLogs;

	private static bool _delayConnections = true;

	public static bool SuppressRejections;

	public static bool SuppressIssued;

	public static uint Rejected;

	public static uint ChallengeIssued;

	public static byte DelayTime = 3;

	internal static byte DelayVolume;

	internal static byte DelayVolumeThreshold;

	public static readonly HashSet<string> UserRateLimit = new HashSet<string>(StringComparer.Ordinal);

	public static readonly HashSet<IPAddress> IpRateLimit = new HashSet<IPAddress>();

	public static readonly HashSet<string> GeoblockingList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static RejectionReason LastRejectionReason;

	public static string LastCustomReason;

	public static string VerificationChallenge;

	public static string VerificationResponse;

	public static long LastBanExpiration;

	public static bool IpPassthroughEnabled;

	public static HashSet<IPAddress> TrustedProxies;

	public static Dictionary<int, string> RealIpAddresses;

	public static uint RejectionThreshold = 60U;

	public static uint IssuedThreshold = 50U;

	private enum ClientType : byte
	{
		GameClient,
		VerificationService
	}
}
