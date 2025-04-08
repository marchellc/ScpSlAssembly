using System;
using CentralAuth;
using GameCore;
using GameObjectPools;
using InventorySystem;
using LabApi.Events.Handlers;
using Mirror;
using ServerOutput;
using UnityEngine;

namespace RoundRestarting
{
	public static class RoundRestart
	{
		public static bool IsRoundRestarting { get; private set; }

		public static int UptimeRounds { get; private set; }

		public static event Action OnRestartTriggered;

		private static int LastRestartTime
		{
			get
			{
				return PlayerPrefsSl.Get("LastRoundrestartTime", 5000);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnServerStarted += RoundRestart.OnServerStarted;
			Inventory.OnLocalClientStarted += RoundRestart.OnClientStarted;
		}

		private static void OnMessageReceived(RoundRestartMessage msg)
		{
		}

		private static void OnClientStarted()
		{
			RoundRestart.IsRoundRestarting = false;
			NetworkClient.ReplaceHandler<RoundRestartMessage>(new Action<RoundRestartMessage>(RoundRestart.OnMessageReceived), true);
		}

		private static void OnServerStarted()
		{
			TimeSpan timeSpan = DateTime.Now - RoundRestart._lastRestartTime;
			if (timeSpan.TotalSeconds > 20.0)
			{
				return;
			}
			PlayerPrefsSl.Set("LastRoundrestartTime", (RoundRestart.LastRestartTime + (int)timeSpan.TotalMilliseconds) / 2);
		}

		public static void InitiateRoundRestart()
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Round restart can only be triggerred by the server!");
			}
			ServerEvents.OnRoundRestarted();
			PoolManager.Singleton.ReturnAllPoolObjects();
			if (RoundRestart.IsRoundRestarting)
			{
				return;
			}
			RoundRestart.IsRoundRestarting = true;
			CustomLiteNetLib4MirrorTransport.DelayConnections = true;
			CustomLiteNetLib4MirrorTransport.UserIdFastReload.Clear();
			IdleMode.PauseIdleMode = true;
			if (ServerStatic.StopNextRound == ServerStatic.NextRoundAction.DoNothing)
			{
				if (CustomNetworkManager.EnableFastRestart)
				{
					foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
					{
						ClientInstanceMode mode = referenceHub.Mode;
						if (mode != ClientInstanceMode.DedicatedServer && mode != ClientInstanceMode.Dummy)
						{
							try
							{
								CustomLiteNetLib4MirrorTransport.UserIdFastReload.Add(referenceHub.authManager.UserId);
							}
							catch (Exception ex)
							{
								ServerConsole.AddLog("Exception occured during processing online player list for Fast Restart: " + ex.Message, ConsoleColor.Yellow, false);
							}
						}
					}
					NetworkServer.SendToAll<RoundRestartMessage>(new RoundRestartMessage(RoundRestartType.FastRestart, 0f, 0, true, true), 0, false);
					RoundRestart.ChangeLevel(false);
					return;
				}
				float num = (float)RoundRestart.LastRestartTime / 1000f;
				NetworkServer.SendToAll<RoundRestartMessage>(new RoundRestartMessage(RoundRestartType.FullRestart, num, 0, true, true), 0, false);
			}
			RoundRestart.ChangeLevel(false);
		}

		internal static void ChangeLevel(bool noShutdownMessage)
		{
			if (!NetworkServer.active)
			{
				NetworkManager.singleton.StopClient();
				return;
			}
			IdleMode.PauseIdleMode = true;
			bool flag = false;
			Action onRestartTriggered = RoundRestart.OnRestartTriggered;
			if (onRestartTriggered != null)
			{
				onRestartTriggered();
			}
			try
			{
				int @int = ConfigFile.ServerConfig.GetInt("restart_after_rounds", 0);
				flag = @int > 0 && RoundRestart.UptimeRounds >= @int;
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Failed to check the restart_after_rounds config value: " + ex.Message, ConsoleColor.Red, false);
			}
			switch (ServerStatic.StopNextRound)
			{
			case ServerStatic.NextRoundAction.DoNothing:
				if (!flag)
				{
					goto IL_0161;
				}
				break;
			case ServerStatic.NextRoundAction.Restart:
				break;
			case ServerStatic.NextRoundAction.Shutdown:
				ServerConsole.AddOutputEntry(default(ExitActionShutdownEntry));
				if (!noShutdownMessage)
				{
					ServerConsole.AddLog("Shutting down the server (StopNextRound command was used)...", ConsoleColor.Gray, false);
				}
				if (ServerStatic.ShutdownRedirectPort != 0)
				{
					if (!noShutdownMessage)
					{
						ServerConsole.AddLog(string.Format("Redirecting players to port {0}...", ServerStatic.ShutdownRedirectPort), ConsoleColor.Gray, false);
					}
					NetworkServer.SendToAll<RoundRestartMessage>(new RoundRestartMessage(RoundRestartType.RedirectRestart, 0.1f, ServerStatic.ShutdownRedirectPort, true, false), 4, true);
					Shutdown.Quit(true, true);
					return;
				}
				Shutdown.Quit(true, false);
				return;
			default:
				goto IL_0161;
			}
			ServerShutdown.ShutdownState = ServerShutdown.ServerShutdownState.Complete;
			ServerConsole.AddOutputEntry(default(ExitActionRestartEntry));
			if (!noShutdownMessage)
			{
				ServerConsole.AddLog(flag ? "Restarting the server (rounds limit set in the server config exceeded)..." : "Restarting the server (RestartNextRound command was used)...", ConsoleColor.Gray, false);
			}
			float num = (float)ConfigFile.ServerConfig.GetInt("full_restart_rejoin_time", 25);
			NetworkServer.SendToAll<RoundRestartMessage>(new RoundRestartMessage(RoundRestartType.FullRestart, num, 0, true, true), 4, true);
			Shutdown.Quit(true, true);
			return;
			IL_0161:
			DummyUtils.DestroyAllDummies();
			GC.Collect();
			RoundRestart._lastRestartTime = DateTime.Now;
			RoundRestart.UptimeRounds++;
			NetworkManager.singleton.ServerChangeScene(NetworkManager.singleton.onlineScene);
		}

		private const string RoundrestartTimeKey = "LastRoundrestartTime";

		private static DateTime _lastRestartTime;
	}
}
