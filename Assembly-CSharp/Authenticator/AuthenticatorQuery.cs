using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CentralAuth;
using GameCore;
using NorthwoodLib;
using RoundRestarting;
using ServerOutput;

namespace Authenticator
{
	public static class AuthenticatorQuery
	{
		public static bool SendData(IEnumerable<string> data)
		{
			bool flag;
			try
			{
				string text = HttpQuery.Post(CentralServer.MasterUrl + "v5/authenticator.php", HttpQuery.ToPostArgs(data));
				flag = (text.StartsWith("{\"") ? AuthenticatorQuery.ProcessResponse(text) : AuthenticatorQuery.ProcessLegacyResponse(text));
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Could not update server data on server list - (LOCAL EXCEPTION) " + ex.Message, ConsoleColor.DarkRed, false);
				flag = false;
			}
			return flag;
		}

		private static void SendContactAddress()
		{
			try
			{
				List<string> list = new List<string>
				{
					"ip=" + ServerConsole.Ip,
					"port=" + ServerConsole.PortToReport.ToString(),
					"version=2",
					"address=" + StringUtils.Base64Encode(ConfigFile.ServerConfig.GetString("contact_email", ""))
				};
				if (!string.IsNullOrEmpty(ServerConsole.Password))
				{
					list.Add("passcode=" + ServerConsole.Password);
				}
				HttpQuery.Post(CentralServer.MasterUrl + "v5/contactaddress.php", HttpQuery.ToPostArgs(list));
			}
			catch
			{
			}
		}

		private static bool ProcessResponse(string response)
		{
			bool flag;
			try
			{
				AuthenticatorResponse authenticatorResponse = JsonSerialize.FromJson<AuthenticatorResponse>(response);
				if (!string.IsNullOrEmpty(authenticatorResponse.verificationChallenge) && !string.IsNullOrEmpty(authenticatorResponse.verificationResponse))
				{
					CustomLiteNetLib4MirrorTransport.VerificationChallenge = authenticatorResponse.verificationChallenge;
					CustomLiteNetLib4MirrorTransport.VerificationResponse = authenticatorResponse.verificationResponse;
					ServerConsole.AddLog("Verification challenge and response have been obtained.\nThe system will attempt to check your server shortly, please allow up to 5 minutes.", ConsoleColor.Green, false);
				}
				if (!authenticatorResponse.success)
				{
					ServerConsole.AddLog("Could not update server data on server list - " + authenticatorResponse.error, ConsoleColor.DarkRed, false);
					flag = false;
				}
				else
				{
					if (!string.IsNullOrEmpty(authenticatorResponse.token))
					{
						ServerConsole.AddLog("Received verification token from central server.", ConsoleColor.Gray, false);
						AuthenticatorQuery.SaveNewToken(authenticatorResponse.token);
					}
					if (authenticatorResponse.actions != null && authenticatorResponse.actions.Length != 0)
					{
						string[] array = authenticatorResponse.actions;
						for (int i = 0; i < array.Length; i++)
						{
							AuthenticatorQuery.HandleAction(array[i]);
						}
					}
					if (authenticatorResponse.messages != null && authenticatorResponse.messages.Length != 0)
					{
						foreach (string text in authenticatorResponse.messages)
						{
							ServerConsole.AddLog("[MESSAGE FROM CENTRAL SERVER] " + text, ConsoleColor.Cyan, false);
						}
					}
					if (authenticatorResponse.authAccepted != null && authenticatorResponse.authAccepted.Length != 0)
					{
						foreach (string text2 in authenticatorResponse.authAccepted)
						{
							ServerConsole.AddLog("Authentication token of player " + text2 + " has been confirmed by central server.", ConsoleColor.Gray, false);
						}
					}
					if (authenticatorResponse.authRejected != null && authenticatorResponse.authRejected.Length != 0)
					{
						foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
						{
							PlayerAuthenticationManager pam = referenceHub.authManager;
							if (!authenticatorResponse.authRejected.All((AuthenticatiorAuthReject rj) => rj.Id != pam.UserId))
							{
								string text3 = authenticatorResponse.authRejected.FirstOrDefault((AuthenticatiorAuthReject rj) => rj.Id == pam.UserId).Reason ?? "<ERROR>";
								ServerConsole.AddLog("Authentication token of player " + pam.UserId + " has been revoked by central server with reason: " + text3, ConsoleColor.Gray, false);
								referenceHub.gameConsoleTransmission.SendToClient(text3, "red");
								ServerConsole.Disconnect(referenceHub.connectionToClient, text3);
							}
						}
					}
					flag = authenticatorResponse.verified;
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Could not update server data on server list - (LOCAL EXCEPTION) " + ex.Message, ConsoleColor.DarkRed, false);
				flag = false;
			}
			return flag;
		}

		private static bool ProcessLegacyResponse(string response)
		{
			if (response == "YES")
			{
				return true;
			}
			if (response.StartsWith("New code generated:"))
			{
				CustomNetworkManager.IsVerified = true;
				string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/verkey.txt";
				try
				{
					File.Delete(text);
				}
				catch
				{
					ServerConsole.AddLog("New password could not be saved.", ConsoleColor.DarkRed, false);
				}
				try
				{
					StreamWriter streamWriter = new StreamWriter(text);
					string text2 = response.Remove(0, response.IndexOf(":", StringComparison.Ordinal)).Remove(response.IndexOf(":", StringComparison.Ordinal));
					while (text2.Contains(":"))
					{
						text2 = text2.Replace(":", string.Empty);
					}
					streamWriter.WriteLine(text2);
					streamWriter.Close();
					ServerConsole.AddLog("New password saved.", ConsoleColor.DarkRed, false);
					ServerConsole.Update = true;
					return true;
				}
				catch
				{
					ServerConsole.AddLog("New password could not be saved.", ConsoleColor.DarkRed, false);
					return true;
				}
			}
			if (response.Contains(":Restart:"))
			{
				AuthenticatorQuery.HandleAction("Restart");
			}
			else if (response.Contains(":RoundRestart:"))
			{
				AuthenticatorQuery.HandleAction("RoundRestart");
			}
			else if (response.Contains(":UpdateData:"))
			{
				AuthenticatorQuery.HandleAction("UpdateData");
			}
			else if (response.Contains(":RefreshKey:"))
			{
				AuthenticatorQuery.HandleAction("RefreshKey");
			}
			else if (response.Contains(":Message - "))
			{
				string text3 = response.Substring(response.IndexOf(":Message - ", StringComparison.Ordinal) + 11);
				text3 = text3.Substring(0, text3.IndexOf(":::", StringComparison.Ordinal));
				ServerConsole.AddLog("[MESSAGE FROM CENTRAL SERVER] " + text3, ConsoleColor.Cyan, false);
			}
			else if (response.Contains(":GetContactAddress:"))
			{
				AuthenticatorQuery.HandleAction("GetContactAddress");
			}
			else
			{
				if (response.Contains("Server is not verified."))
				{
					return false;
				}
				ServerConsole.AddLog("Could not update data on server list (legacy)- " + response, ConsoleColor.DarkRed, false);
			}
			return true;
		}

		internal static void HandleAction(string action)
		{
			if (!(action == "Restart"))
			{
				if (!(action == "RoundRestart"))
				{
					if (action == "UpdateData")
					{
						ServerConsole.Update = true;
						return;
					}
					if (action == "RefreshKey")
					{
						ServerConsole.RunRefreshPublicKeyOnce();
						return;
					}
					if (!(action == "GetContactAddress"))
					{
						return;
					}
					new Thread(new ThreadStart(AuthenticatorQuery.SendContactAddress))
					{
						Name = "SCP:SL Response to central servers (contact data request)",
						Priority = ThreadPriority.BelowNormal,
						IsBackground = true
					}.Start();
				}
				else
				{
					ServerConsole.AddLog("Round restart requested by central server.", ConsoleColor.DarkRed, false);
					ReferenceHub referenceHub;
					if (ReferenceHub.TryGetLocalHub(out referenceHub) && referenceHub.networkIdentity.isServer)
					{
						RoundRestart.InitiateRoundRestart();
						return;
					}
				}
				return;
			}
			ServerConsole.AddOutputEntry(default(ExitActionRestartEntry));
			ServerConsole.AddLog("Server restart requested by central server.", ConsoleColor.DarkRed, false);
			Shutdown.Quit(true, false);
		}

		private static void SaveNewToken(string token)
		{
			CustomNetworkManager.IsVerified = true;
			string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/verkey.txt";
			try
			{
				File.Delete(text);
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("New verification token could not be saved (1): " + ex.Message, ConsoleColor.DarkRed, false);
			}
			try
			{
				StreamWriter streamWriter = new StreamWriter(text);
				streamWriter.WriteLine(token);
				streamWriter.Close();
				ServerConsole.AddLog("New verification token saved.", ConsoleColor.DarkRed, false);
				ServerConsole.Update = true;
				ServerConsole.ScheduleTokenRefresh = true;
			}
			catch (Exception ex2)
			{
				ServerConsole.AddLog("New verification token could not be saved (2): " + ex2.Message, ConsoleColor.DarkRed, false);
			}
		}
	}
}
