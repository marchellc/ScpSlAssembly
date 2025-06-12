using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameCore;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace AFK;

public static class AFKManager
{
	public delegate void AFKKick(ReferenceHub userHub);

	private static readonly Dictionary<ReferenceHub, Stopwatch> AFKTimers = new Dictionary<ReferenceHub, Stopwatch>();

	private static float _kickTime;

	private static bool _constantlyCheck;

	private static string _kickMessage;

	private static bool _eventsStatus;

	public static event AFKKick OnAFKKick;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void Init()
	{
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(ConfigReloaded));
	}

	private static void ConfigReloaded()
	{
		AFKManager._constantlyCheck = ConfigFile.ServerConfig.GetBool("constantly_check_afk");
		AFKManager._kickTime = ConfigFile.ServerConfig.GetFloat("afk_time", 90f);
		AFKManager._kickMessage = ConfigFile.ServerConfig.GetString("afk_kick_message", "AFK");
		if (AFKManager._kickTime <= 0f)
		{
			if (AFKManager._eventsStatus)
			{
				AFKManager._eventsStatus = false;
				ReferenceHub.OnPlayerAdded -= AddPlayer;
				ReferenceHub.OnPlayerRemoved -= RemovePlayer;
				StaticUnityMethods.OnUpdate -= OnUpdate;
				PlayerRoleManager.OnRoleChanged -= RoleChange;
			}
		}
		else if (!AFKManager._eventsStatus)
		{
			AFKManager._eventsStatus = true;
			ReferenceHub.OnPlayerAdded += AddPlayer;
			ReferenceHub.OnPlayerRemoved += RemovePlayer;
			StaticUnityMethods.OnUpdate += OnUpdate;
			PlayerRoleManager.OnRoleChanged += RoleChange;
		}
	}

	public static void AddPlayer(ReferenceHub hub)
	{
		if (NetworkServer.active && !(hub == ReferenceHub.HostHub) && !AFKManager.AFKTimers.ContainsKey(hub) && !PermissionsHandler.IsPermitted(hub.serverRoles.Permissions, PlayerPermissions.AFKImmunity))
		{
			AFKManager.AFKTimers.Add(hub, Stopwatch.StartNew());
		}
	}

	private static void RemovePlayer(ReferenceHub hub)
	{
		AFKManager.AFKTimers.Remove(hub);
	}

	private static void RoleChange(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (NetworkServer.active && AFKManager.AFKTimers.TryGetValue(hub, out var value))
		{
			if (PermissionsHandler.IsPermitted(hub.serverRoles.Permissions, PlayerPermissions.AFKImmunity) || hub == ReferenceHub.HostHub)
			{
				AFKManager.AFKTimers.Remove(hub);
			}
			else
			{
				value.Restart();
			}
		}
	}

	private static void OnUpdate()
	{
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!AFKManager.AFKTimers.TryGetValue(allHub, out var value) || !value.IsRunning || !(allHub.roleManager.CurrentRole is IAFKRole iAFKRole))
			{
				continue;
			}
			if (!iAFKRole.IsAFK)
			{
				if (AFKManager._constantlyCheck)
				{
					value.Restart();
				}
				else
				{
					value.Reset();
				}
			}
			else if (value.Elapsed.TotalSeconds >= (double)AFKManager._kickTime)
			{
				value.Reset();
				AFKManager.OnAFKKick?.Invoke(allHub);
				BanPlayer.KickUser(allHub, AFKManager._kickMessage);
			}
		}
	}
}
