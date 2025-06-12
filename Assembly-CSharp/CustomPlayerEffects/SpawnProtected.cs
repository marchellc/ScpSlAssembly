using System;
using System.Collections.Generic;
using GameCore;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects;

public class SpawnProtected : StatusEffectBase, ISpectatorDataPlayerEffect, ICustomRADisplay, IDamageModifierEffect, IPulseEffect
{
	public static bool IsProtectionEnabled;

	public static bool CanShoot;

	public static bool PreventAllDamage;

	public static float SpawnDuration;

	public static readonly List<Team> ProtectedTeams = new List<Team>();

	private static bool _eventAssigned = false;

	public string DisplayName => "Spawn Protection";

	public bool CanBeDisplayed => true;

	public bool DamageModifierActive => base.IsEnabled;

	public override EffectClassification Classification => EffectClassification.Positive;

	internal override void OnRoleChanged(PlayerRoleBase previousRole, PlayerRoleBase newRole)
	{
		if (!SpawnProtected.TryGiveProtection(base.Hub))
		{
			base.OnRoleChanged(previousRole, newRole);
		}
	}

	public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
	{
		if (!SpawnProtected.IsProtectionEnabled)
		{
			return 1f;
		}
		if (!(handler is AttackerDamageHandler attackerDamageHandler))
		{
			if (!SpawnProtected.PreventAllDamage)
			{
				return 1f;
			}
			return this.CancelDamage();
		}
		if (!(attackerDamageHandler.Attacker.Hub == base.Hub))
		{
			return this.CancelDamage();
		}
		return 1f;
	}

	public bool GetSpectatorText(out string display)
	{
		display = "Spawn protected";
		return base.IsEnabled;
	}

	public void ExecutePulse()
	{
	}

	private float CancelDamage()
	{
		base.Hub.playerEffectsController.ServerSendPulse<SpawnProtected>();
		return 0f;
	}

	public static bool CheckPlayer(ReferenceHub hub)
	{
		if (NetworkServer.active && !SpawnProtected.IsProtectionEnabled)
		{
			return false;
		}
		return hub.playerEffectsController.GetEffect<SpawnProtected>().IsEnabled;
	}

	public static bool TryGiveProtection(ReferenceHub hub)
	{
		if (!NetworkServer.active || !SpawnProtected.IsProtectionEnabled)
		{
			return false;
		}
		PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
		if (!(currentRole is IHealthbarRole))
		{
			return false;
		}
		if (!SpawnProtected.ProtectedTeams.Contains(currentRole.Team))
		{
			return false;
		}
		hub.playerEffectsController.EnableEffect<SpawnProtected>(SpawnProtected.SpawnDuration);
		return true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		if (!SpawnProtected._eventAssigned)
		{
			ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(RefreshConfigs));
			ServerConfigSynchronizer.OnRefreshed = (Action)Delegate.Combine(ServerConfigSynchronizer.OnRefreshed, new Action(RefreshConfigs));
			SpawnProtected._eventAssigned = true;
		}
		SpawnProtected.RefreshConfigs();
	}

	private static void RefreshConfigs()
	{
		SpawnProtected.ProtectedTeams.Clear();
		SpawnProtected.IsProtectionEnabled = ConfigFile.ServerConfig.GetBool("spawn_protect_enabled");
		SpawnProtected.CanShoot = ConfigFile.ServerConfig.GetBool("spawn_protect_can_shoot");
		SpawnProtected.PreventAllDamage = ConfigFile.ServerConfig.GetBool("spawn_protect_prevent_all");
		SpawnProtected.SpawnDuration = ConfigFile.ServerConfig.GetFloat("spawn_protect_time", 8f);
		List<int> intList = ConfigFile.ServerConfig.GetIntList("spawn_protect_team");
		if (intList.Count == 0)
		{
			SpawnProtected.ProtectedTeams.Add(Team.FoundationForces);
			SpawnProtected.ProtectedTeams.Add(Team.ChaosInsurgency);
		}
		else
		{
			intList.ForEach(delegate(int t)
			{
				SpawnProtected.ProtectedTeams.Add((Team)t);
			});
		}
	}
}
