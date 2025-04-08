using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CentralAuth;
using LightContainmentZoneDecontamination;
using Query;
using Security;
using UnityEngine;

namespace GameCore
{
	public static class ConfigFile
	{
		static ConfigFile()
		{
			ServerStatic.ProcessServerArgs();
			if (!Directory.Exists(FileManager.GetAppFolder(true, false, "")))
			{
				Directory.CreateDirectory(FileManager.GetAppFolder(true, false, ""));
			}
			if (File.Exists(FileManager.GetAppFolder(true, false, "") + "config.txt") && !File.Exists(FileManager.GetAppFolder(true, false, "") + "LEGACY CONFIG BACKUP - NOT WORKING.txt"))
			{
				File.Move(FileManager.GetAppFolder(true, false, "") + "config.txt", FileManager.GetAppFolder(true, false, "") + "LEGACY CONFIG BACKUP - NOT WORKING.txt");
			}
			ConfigFile.ReloadGameConfigs(true);
		}

		public static void ReloadGameConfigs(bool firstTime = false)
		{
			if (firstTime && ConfigFile._loaded)
			{
				return;
			}
			ConfigFile._loaded = true;
			ServerConsole.AddLog("Loading gameplay config...", ConsoleColor.Gray, false);
			string configPath = ConfigFile.GetConfigPath("config_gameplay");
			if (ConfigFile.ServerConfig == null)
			{
				ConfigFile.ServerConfig = new YamlConfig(configPath);
			}
			else
			{
				ConfigFile.ServerConfig.LoadConfigFile(configPath);
			}
			ServerConsole.RefreshEmailSetStatus();
			ServerConsole.AddLog("Processing rate limits...", ConsoleColor.Gray, false);
			RateLimitCreator.Load();
			ServerConsole.AddLog("Loading sharing config...", ConsoleColor.Gray, false);
			string configPath2 = ConfigFile.GetConfigPath("config_sharing");
			if (ConfigFile.SharingConfig == null)
			{
				ConfigFile.SharingConfig = new YamlConfig(configPath2);
			}
			else
			{
				ConfigFile.SharingConfig.LoadConfigFile(configPath2);
			}
			ServerConsole.AddLog("Processing shares...", ConsoleColor.Gray, false);
			ConfigSharing.Reload();
			BanHandler.Init();
			WhiteList.Reload();
			ReservedSlot.Reload();
			QueryServer.ReloadConfig();
			ServerStatic.ServerTickrate = ConfigFile.ServerConfig.GetShort("server_tickrate", 60);
			IdleMode.IdleModeEnabled = ConfigFile.ServerConfig.GetBool("idle_mode_enabled", true);
			IdleMode.IdleModeTime = ConfigFile.ServerConfig.GetUInt("idle_mode_time", 5000U);
			IdleMode.IdleModePreauthTime = ConfigFile.ServerConfig.GetUInt("idle_mode_preauth_time", 30000U);
			IdleMode.IdleModeTickrate = ConfigFile.ServerConfig.GetShort("idle_mode_tickrate", 1);
			ServerConsole.PortOverride = ConfigFile.ServerConfig.GetUShort("server_list_port_override", 0);
			ServerConsole.FriendlyFire = ConfigFile.ServerConfig.GetBool("friendly_fire", false);
			ServerConsole.WhiteListEnabled = ConfigFile.ServerConfig.GetBool("enable_whitelist", false) || ConfigFile.ServerConfig.GetBool("custom_whitelist", false);
			ServerConsole.AccessRestriction = ConfigFile.ServerConfig.GetBool("server_access_restriction", false);
			ServerConsole.TransparentlyModdedServerConfig = ConfigFile.ServerConfig.GetBool("transparently_modded_server", false);
			ServerConsole.RateLimitKick = ConfigFile.ServerConfig.GetBool("ratelimit_kick", true);
			ServerConsole.EnforceSameIp = ConfigFile.ServerConfig.GetBool("enforce_same_ip", true);
			ServerConsole.SkipEnforcementForLocalAddresses = ConfigFile.ServerConfig.GetBool("no_enforcement_for_local_ip_addresses", true);
			CustomNetworkManager.EnableFastRestart = ConfigFile.ServerConfig.GetBool("enable_fast_round_restart", false);
			CustomNetworkManager.FastRestartDelay = ConfigFile.ServerConfig.GetFloat("fast_round_restart_delay", 3.2f);
			PlayerAuthenticationManager.OnlineMode = ConfigFile.ServerConfig.GetBool("online_mode", true);
			PlayerAuthenticationManager.AuthenticationTimeout = ConfigFile.ServerConfig.GetUInt("authentication_timeout", 45U);
			PlayerAuthenticationManager.AllowSameAccountJoining = ConfigFile.ServerConfig.GetBool("same_account_joining", false);
			EncryptedChannelManager.CryptographyDebug = ConfigFile.ServerConfig.GetBool("enable_crypto_debug", false);
			if (EncryptedChannelManager.CryptographyDebug)
			{
				ServerConsole.AddLog("WARNING - Cryptography Debug is enabled! THIS IS A SECURITY RISK if used on a public server (admins can seriously abuse this feature)! You can disable this by setting 'enable_crypto_debug' to false in your gameplay config file.", ConsoleColor.Yellow, false);
			}
			CharacterClassManager.CuffedChangeTeam = ConfigFile.ServerConfig.GetBool("cuffed_escapee_change_team", true);
			CharacterClassManager.EnableSyncServerCmdBinding = ConfigFile.ServerConfig.GetBool("enable_sync_command_binding", false);
			PocketDimensionTeleport.RefreshExit = ConfigFile.ServerConfig.GetBool("pd_refresh_exit", false);
			AlphaWarheadController.AutoWarheadBroadcastEnabled = ConfigFile.ServerConfig.GetBool("auto_warhead_broadcast_enabled", true);
			AlphaWarheadController.WarheadBroadcastMessage = ConfigFile.ServerConfig.GetString("auto_warhead_broadcast_message", "The Alpha Warhead is being detonated");
			AlphaWarheadController.WarheadBroadcastMessageTime = ConfigFile.ServerConfig.GetUShort("auto_warhead_broadcast_time", 10);
			AlphaWarheadController.WarheadExplodedBroadcastMessage = ConfigFile.ServerConfig.GetString("auto_warhead_detonate_broadcast", "The Alpha Warhead has been detonated");
			AlphaWarheadController.WarheadExplodedBroadcastMessageTime = ConfigFile.ServerConfig.GetUShort("auto_warhead_detonate_broadcast_time", 10);
			AlphaWarheadController.LockGatesOnCountdown = ConfigFile.ServerConfig.GetBool("lock_gates_on_countdown", true);
			DecontaminationController.AutoDeconBroadcastEnabled = ConfigFile.ServerConfig.GetBool("auto_decon_broadcast_enabled", false);
			DecontaminationController.DeconBroadcastDeconMessage = ConfigFile.ServerConfig.GetString("auto_decon_broadcast_message", "Light Containment Zone is being decontaminated");
			DecontaminationController.DeconBroadcastDeconMessageTime = ConfigFile.ServerConfig.GetUShort("auto_decon_broadcast_time", 10);
			CustomLiteNetLib4MirrorTransport.DisplayPreauthLogs = ConfigFile.ServerConfig.GetBool("display_preauth_logs", true);
			CustomLiteNetLib4MirrorTransport.DelayTime = ConfigFile.ServerConfig.GetByte("connections_delay_time", 5);
			CustomLiteNetLib4MirrorTransport.IpRateLimiting = ConfigFile.ServerConfig.GetBool("enable_ip_ratelimit", true);
			CustomLiteNetLib4MirrorTransport.UserRateLimiting = ConfigFile.ServerConfig.GetBool("enable_userid_ratelimit", true);
			CustomLiteNetLib4MirrorTransport.GeoblockIgnoreWhitelisted = ConfigFile.ServerConfig.GetBool("geoblocking_ignore_whitelisted", true);
			CustomLiteNetLib4MirrorTransport.RejectionThreshold = ConfigFile.ServerConfig.GetUInt("rejection_suppression_threshold", 60U);
			CustomLiteNetLib4MirrorTransport.IssuedThreshold = ConfigFile.ServerConfig.GetUInt("challenge_issuance_suppression_threshold", 50U);
			CustomLiteNetLib4MirrorTransport.ReloadChallengeOptions();
			CustomLiteNetLib4MirrorTransport.GeoblockingList.Clear();
			string @string = ConfigFile.ServerConfig.GetString("geoblocking_mode", "none");
			if (!(@string == "whitelist"))
			{
				if (!(@string == "blacklist"))
				{
					goto IL_04F9;
				}
			}
			else
			{
				CustomLiteNetLib4MirrorTransport.Geoblocking = GeoblockingMode.Whitelist;
				using (List<string>.Enumerator enumerator = ConfigFile.ServerConfig.GetStringList("geoblocking_whitelist").GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string text = enumerator.Current;
						CustomLiteNetLib4MirrorTransport.GeoblockingList.Add(text);
					}
					goto IL_04FF;
				}
			}
			CustomLiteNetLib4MirrorTransport.Geoblocking = GeoblockingMode.Blacklist;
			using (List<string>.Enumerator enumerator = ConfigFile.ServerConfig.GetStringList("geoblocking_blacklist").GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					string text2 = enumerator.Current;
					CustomLiteNetLib4MirrorTransport.GeoblockingList.Add(text2);
				}
				goto IL_04FF;
			}
			IL_04F9:
			CustomLiteNetLib4MirrorTransport.Geoblocking = GeoblockingMode.None;
			IL_04FF:
			CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled = PlayerAuthenticationManager.OnlineMode && ConfigFile.ServerConfig.GetBool("enable_proxy_ip_passthrough", false);
			if (CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled)
			{
				if (CustomLiteNetLib4MirrorTransport.TrustedProxies == null)
				{
					CustomLiteNetLib4MirrorTransport.TrustedProxies = new HashSet<IPAddress>();
				}
				else
				{
					CustomLiteNetLib4MirrorTransport.TrustedProxies.Clear();
				}
				foreach (string text3 in ConfigFile.ServerConfig.GetStringList("trusted_proxies_ip_addresses"))
				{
					IPAddress ipaddress;
					if (IPAddress.TryParse(text3, out ipaddress))
					{
						CustomLiteNetLib4MirrorTransport.TrustedProxies.Add(ipaddress);
					}
					else
					{
						ServerConsole.AddLog("Couldn't parse trusted proxy IP address: " + text3, ConsoleColor.Red, false);
					}
				}
				if (CustomLiteNetLib4MirrorTransport.TrustedProxies.Count == 0)
				{
					CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled = false;
					CustomLiteNetLib4MirrorTransport.TrustedProxies = null;
					CustomLiteNetLib4MirrorTransport.RealIpAddresses = null;
				}
				else if (CustomLiteNetLib4MirrorTransport.RealIpAddresses == null)
				{
					CustomLiteNetLib4MirrorTransport.RealIpAddresses = new Dictionary<int, string>();
				}
			}
			else
			{
				CustomLiteNetLib4MirrorTransport.TrustedProxies = null;
				CustomLiteNetLib4MirrorTransport.RealIpAddresses = null;
			}
			CheaterReport.WebhookUrl = ConfigFile.ServerConfig.GetString("report_discord_webhook_url", string.Empty);
			CheaterReport.WebhookUsername = ConfigFile.ServerConfig.GetString("report_username", "Cheater Report");
			CheaterReport.WebhookAvatar = ConfigFile.ServerConfig.GetString("report_avatar_url", string.Empty);
			CheaterReport.WebhookColor = ConfigFile.ServerConfig.GetInt("report_color", 14423100);
			CheaterReport.ServerName = ConfigFile.ServerConfig.GetString("report_server_name", "My SCP:SL Server");
			CheaterReport.ReportHeader = ConfigFile.ServerConfig.GetString("report_header", "Player Report");
			CheaterReport.ReportContent = ConfigFile.ServerConfig.GetString("report_content", "Player has just been reported.");
			CheaterReport.SendReportsByWebhooks = ConfigFile.ServerConfig.GetBool("report_send_using_discord_webhook", false) && !string.IsNullOrWhiteSpace(CheaterReport.WebhookUrl) && !string.IsNullOrWhiteSpace(CheaterReport.WebhookUsername);
			PlayerInteract.Scp096DestroyLockedDoors = ConfigFile.ServerConfig.GetBool("096_destroy_locked_doors", true);
			PlayerInteract.CanDisarmedInteract = ConfigFile.ServerConfig.GetBool("allow_disarmed_interaction", false);
			FriendlyFireConfig.RoundEnabled = ConfigFile.ServerConfig.GetBool("ff_detector_round_enabled", true);
			FriendlyFireConfig.LifeEnabled = ConfigFile.ServerConfig.GetBool("ff_detector_life_enabled", true);
			FriendlyFireConfig.WindowEnabled = ConfigFile.ServerConfig.GetBool("ff_detector_window_enabled", true);
			FriendlyFireConfig.RespawnEnabled = ConfigFile.ServerConfig.GetBool("ff_detector_spawn_enabled", true);
			FriendlyFireConfig.ExplosionAfterDisconnecting = ConfigFile.ServerConfig.GetBool("ff_detector_explosion_after_disconnecting_enabled", true);
			FriendlyFireConfig.RoundAction = FriendlyFireConfig.ParseAction(ConfigFile.ServerConfig.GetString("ff_detector_round_action", "ban"));
			FriendlyFireConfig.LifeAction = FriendlyFireConfig.ParseAction(ConfigFile.ServerConfig.GetString("ff_detector_life_action", "ban"));
			FriendlyFireConfig.WindowAction = FriendlyFireConfig.ParseAction(ConfigFile.ServerConfig.GetString("ff_detector_window_action", "ban"));
			FriendlyFireConfig.RespawnAction = FriendlyFireConfig.ParseAction(ConfigFile.ServerConfig.GetString("ff_detector_spawn_action", "ban"));
			FriendlyFireConfig.ExplosionAfterDisconnectingAction = FriendlyFireConfig.ParseAction(ConfigFile.ServerConfig.GetString("ff_detector_explosion_after_disconnecting_action", "ban"));
			if (FriendlyFireConfig.ExplosionAfterDisconnectingAction == FriendlyFireAction.Kick || FriendlyFireConfig.ExplosionAfterDisconnectingAction == FriendlyFireAction.Kill)
			{
				FriendlyFireConfig.ExplosionAfterDisconnectingAction = FriendlyFireAction.Noop;
				Console.AddLog("Actions \"Kick\" and \"Kill\" are invalid for \"ff_detector_explosion_after_disconnecting_action\". Replaced with \"noop\".", Color.red, false, Console.ConsoleLogType.Log);
			}
			FriendlyFireConfig.RoundKillThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_round_kills", 6U);
			FriendlyFireConfig.LifeKillThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_life_kills", 4U);
			FriendlyFireConfig.WindowKillThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_window_kills", 3U);
			FriendlyFireConfig.RespawnKillThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_spawn_kills", 2U);
			FriendlyFireConfig.RoundDamageThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_round_damage", 500U);
			FriendlyFireConfig.LifeDamageThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_life_damage", 300U);
			FriendlyFireConfig.WindowDamageThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_window_damage", 250U);
			FriendlyFireConfig.RespawnDamageThreshold = ConfigFile.ServerConfig.GetUInt("ff_detector_spawn_damage", 180U);
			try
			{
				FriendlyFireConfig.RoundBanTime = Misc.RelativeTimeToSeconds(ConfigFile.ServerConfig.GetString("ff_detector_round_ban_time", "24h"), 1);
				FriendlyFireConfig.LifeBanTime = Misc.RelativeTimeToSeconds(ConfigFile.ServerConfig.GetString("ff_detector_life_ban_time", "24h"), 1);
				FriendlyFireConfig.WindowBanTime = Misc.RelativeTimeToSeconds(ConfigFile.ServerConfig.GetString("ff_detector_window_ban_time", "16h"), 1);
				FriendlyFireConfig.RespawnBanTime = Misc.RelativeTimeToSeconds(ConfigFile.ServerConfig.GetString("ff_detector_spawn_ban_time", "48h"), 1);
				FriendlyFireConfig.ExplosionAfterDisconnectingTime = Misc.RelativeTimeToSeconds(ConfigFile.ServerConfig.GetString("ff_detector_explosion_after_disconnecting_ban_time", "48h"), 1);
			}
			catch
			{
				FriendlyFireConfig.RoundBanTime = 86400L;
				FriendlyFireConfig.LifeBanTime = 86400L;
				FriendlyFireConfig.WindowBanTime = 57600L;
				FriendlyFireConfig.RespawnBanTime = 172800L;
				FriendlyFireConfig.ExplosionAfterDisconnectingTime = 172800L;
				Console.AddLog("Failed to parse Friendly Fire Detector ban times. Using default values...", Color.red, false, Console.ConsoleLogType.Log);
			}
			FriendlyFireConfig.RoundBanReason = ConfigFile.ServerConfig.GetString("ff_detector_round_bankick_reason", "You have been automatically banned for teamkilling.");
			FriendlyFireConfig.LifeBanReason = ConfigFile.ServerConfig.GetString("ff_detector_life_bankick_reason", "You have been automatically banned for teamkilling.");
			FriendlyFireConfig.WindowBanReason = ConfigFile.ServerConfig.GetString("ff_detector_window_bankick_reason", "You have been automatically banned for teamkilling.");
			FriendlyFireConfig.RespawnBanReason = ConfigFile.ServerConfig.GetString("ff_detector_spawn_bankick_reason", "You have been automatically banned for teamkilling.");
			FriendlyFireConfig.ExplosionAfterDisconnectingBanReason = ConfigFile.ServerConfig.GetString("ff_detector_explosion_after_disconnecting_bankick_reason", "You have been automatically banned for teamkilling.");
			FriendlyFireConfig.RoundKillReason = ConfigFile.ServerConfig.GetString("ff_detector_round_kill_reason", "You have been automatically killed for teamkilling.");
			FriendlyFireConfig.LifeKillReason = ConfigFile.ServerConfig.GetString("ff_detector_life_kill_reason", "You have been automatically killed for teamkilling.");
			FriendlyFireConfig.WindowKillReason = ConfigFile.ServerConfig.GetString("ff_detector_window_kill_reason", "You have been automatically killed for teamkilling.");
			FriendlyFireConfig.RespawnKillReason = ConfigFile.ServerConfig.GetString("ff_detector_spawn_kill_reason", "You have been automatically killed for teamkilling.");
			FriendlyFireConfig.RoundAdminMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_round_adminchat_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_round_adminchat_message", "%nick has been banned for teamkilling (round detector).") : null);
			FriendlyFireConfig.LifeAdminMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_life_adminchat_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_life_adminchat_message", "%nick has been banned for teamkilling (life detector).") : null);
			FriendlyFireConfig.WindowAdminMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_window_adminchat_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_window_adminchat_message", "%nick has been banned for teamkilling (window detector).") : null);
			FriendlyFireConfig.RespawnAdminMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_spawn_adminchat_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_spawn_adminchat_message", "%nick has been banned for teamkilling (spawn detector).") : null);
			FriendlyFireConfig.ExplosionAfterDisconnectingAdminMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_explosion_after_disconnecting_adminchat_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_explosion_after_disconnecting_adminchat_message", "%nick has been banned for teamkilling (explosion after disconnecting detector).") : null);
			FriendlyFireConfig.RoundBroadcastMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_round_broadcast_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_round_broadcast_message", "%nick has been automatically banned for teamkilling.") : null);
			FriendlyFireConfig.LifeBroadcastMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_life_broadcast_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_life_broadcast_message", "%nick has been automatically banned for teamkilling.") : null);
			FriendlyFireConfig.WindowBroadcastMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_window_broadcast_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_window_broadcast_message", "%nick has been automatically banned for teamkilling.") : null);
			FriendlyFireConfig.RespawnBroadcastMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_spawn_broadcast_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_spawn_broadcast_message", "%nick has been automatically banned for teamkilling.") : null);
			FriendlyFireConfig.ExplosionAfterDisconnectingBroadcastMessage = (ConfigFile.ServerConfig.GetBool("ff_detector_explosion_after_disconnecting_broadcast_enable", false) ? ConfigFile.ServerConfig.GetString("ff_detector_explosion_after_disconnecting_broadcast_message", "%nick has been automatically banned for teamkilling.") : null);
			FriendlyFireConfig.RoundWebhook = ConfigFile.ServerConfig.GetBool("ff_detector_round_webhook_report", true);
			FriendlyFireConfig.LifeWebhook = ConfigFile.ServerConfig.GetBool("ff_detector_life_webhook_report", true);
			FriendlyFireConfig.WindowWebhook = ConfigFile.ServerConfig.GetBool("ff_detector_window_webhook_report", true);
			FriendlyFireConfig.RespawnWebhook = ConfigFile.ServerConfig.GetBool("ff_detector_spawn_webhook_report", true);
			FriendlyFireConfig.ExplosionAfterDisconnectingWebhook = ConfigFile.ServerConfig.GetBool("ff_detector_explosion_after_disconnecting_webhook_report", true);
			FriendlyFireConfig.BroadcastTime = ConfigFile.ServerConfig.GetUShort("ff_detector_global_broadcast_seconds", 5);
			FriendlyFireConfig.AdminChatTime = ConfigFile.ServerConfig.GetUShort("ff_detector_global_adminchat_seconds", 6);
			FriendlyFireConfig.Window = ConfigFile.ServerConfig.GetUInt("ff_detector_window_seconds", 180U);
			FriendlyFireConfig.RespawnWindow = ConfigFile.ServerConfig.GetUInt("ff_detector_spawn_window_seconds", 120U);
			FriendlyFireConfig.IgnoreClassDTeamkills = ConfigFile.ServerConfig.GetBool("ff_detector_classD_can_damage_classD", false);
			FriendlyFireConfig.WebhookUrl = ConfigFile.ServerConfig.GetString("ff_detector_webhook_url", "none");
			if (FriendlyFireConfig.WebhookUrl == "none")
			{
				FriendlyFireConfig.WebhookUrl = CheaterReport.WebhookUrl;
			}
			CustomNetworkManager.ReloadTimeWindows();
			ServerConsole.ReloadServerName();
			ServerConfigSynchronizer.RefreshAllConfigs();
			Action onConfigReloaded = ConfigFile.OnConfigReloaded;
			if (onConfigReloaded == null)
			{
				return;
			}
			onConfigReloaded();
		}

		public static string GetConfigPath(string name)
		{
			string appFolder = FileManager.GetAppFolder(true, true, "");
			if (!Directory.Exists(appFolder))
			{
				Directory.CreateDirectory(appFolder);
			}
			if (File.Exists(appFolder + name + ".txt"))
			{
				return appFolder + name + ".txt";
			}
			try
			{
				File.Copy("ConfigTemplates/" + name + ".template.txt", appFolder + name + ".txt");
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Error during copying config file: " + ex.Message, ConsoleColor.Gray, false);
				return null;
			}
			return appFolder + name + ".txt";
		}

		public static Action OnConfigReloaded;

		public static YamlConfig ServerConfig;

		public static YamlConfig SharingConfig;

		public static YamlConfig HosterPolicy;

		private static bool _loaded;
	}
}
