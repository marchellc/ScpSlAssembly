using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameCore;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace AFK
{
	public static class AFKManager
	{
		public static event AFKManager.AFKKick OnAFKKick;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void Init()
		{
			ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(AFKManager.ConfigReloaded));
		}

		private static void ConfigReloaded()
		{
			AFKManager._constantlyCheck = ConfigFile.ServerConfig.GetBool("constantly_check_afk", false);
			AFKManager._kickTime = ConfigFile.ServerConfig.GetFloat("afk_time", 90f);
			AFKManager._kickMessage = ConfigFile.ServerConfig.GetString("afk_kick_message", "AFK");
			if (AFKManager._kickTime > 0f)
			{
				if (!AFKManager._eventsStatus)
				{
					AFKManager._eventsStatus = true;
					ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(AFKManager.AddPlayer));
					ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(AFKManager.RemovePlayer));
					StaticUnityMethods.OnUpdate += AFKManager.OnUpdate;
					PlayerRoleManager.OnRoleChanged += AFKManager.RoleChange;
				}
				return;
			}
			if (!AFKManager._eventsStatus)
			{
				return;
			}
			AFKManager._eventsStatus = false;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(AFKManager.AddPlayer));
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(AFKManager.RemovePlayer));
			StaticUnityMethods.OnUpdate -= AFKManager.OnUpdate;
			PlayerRoleManager.OnRoleChanged -= AFKManager.RoleChange;
		}

		public static void AddPlayer(ReferenceHub hub)
		{
			if (!NetworkServer.active || hub == ReferenceHub.HostHub || AFKManager.AFKTimers.ContainsKey(hub))
			{
				return;
			}
			if (PermissionsHandler.IsPermitted(hub.serverRoles.Permissions, PlayerPermissions.AFKImmunity))
			{
				return;
			}
			AFKManager.AFKTimers.Add(hub, Stopwatch.StartNew());
		}

		private static void RemovePlayer(ReferenceHub hub)
		{
			AFKManager.AFKTimers.Remove(hub);
		}

		private static void RoleChange(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Stopwatch stopwatch;
			if (!AFKManager.AFKTimers.TryGetValue(hub, out stopwatch))
			{
				return;
			}
			if (PermissionsHandler.IsPermitted(hub.serverRoles.Permissions, PlayerPermissions.AFKImmunity) || hub == ReferenceHub.HostHub)
			{
				AFKManager.AFKTimers.Remove(hub);
				return;
			}
			stopwatch.Restart();
		}

		private static void OnUpdate()
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Stopwatch stopwatch;
				if (AFKManager.AFKTimers.TryGetValue(referenceHub, out stopwatch) && stopwatch.IsRunning)
				{
					IAFKRole iafkrole = referenceHub.roleManager.CurrentRole as IAFKRole;
					if (iafkrole != null)
					{
						if (!iafkrole.IsAFK)
						{
							if (AFKManager._constantlyCheck)
							{
								stopwatch.Restart();
							}
							else
							{
								stopwatch.Reset();
							}
						}
						else if (stopwatch.Elapsed.TotalSeconds >= (double)AFKManager._kickTime)
						{
							stopwatch.Reset();
							AFKManager.AFKKick onAFKKick = AFKManager.OnAFKKick;
							if (onAFKKick != null)
							{
								onAFKKick(referenceHub);
							}
							BanPlayer.KickUser(referenceHub, AFKManager._kickMessage);
						}
					}
				}
			}
		}

		private static readonly Dictionary<ReferenceHub, Stopwatch> AFKTimers = new Dictionary<ReferenceHub, Stopwatch>();

		private static float _kickTime;

		private static bool _constantlyCheck;

		private static string _kickMessage;

		private static bool _eventsStatus;

		public delegate void AFKKick(ReferenceHub userHub);
	}
}
