using System;
using System.Collections.Generic;
using GameCore;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class SpawnProtected : StatusEffectBase, ISpectatorDataPlayerEffect, ICustomRADisplay, IDamageModifierEffect, IPulseEffect
	{
		public string DisplayName
		{
			get
			{
				return "Spawn Protection";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

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
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				if (!SpawnProtected.PreventAllDamage)
				{
					return 1f;
				}
				return this.CancelDamage();
			}
			else
			{
				if (!(attackerDamageHandler.Attacker.Hub == base.Hub))
				{
					return this.CancelDamage();
				}
				return 1f;
			}
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
			return (!NetworkServer.active || SpawnProtected.IsProtectionEnabled) && hub.playerEffectsController.GetEffect<SpawnProtected>().IsEnabled;
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
			hub.playerEffectsController.EnableEffect<SpawnProtected>(SpawnProtected.SpawnDuration, false);
			return true;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			if (!SpawnProtected._eventAssigned)
			{
				ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(SpawnProtected.RefreshConfigs));
				ServerConfigSynchronizer.OnRefreshed = (Action)Delegate.Combine(ServerConfigSynchronizer.OnRefreshed, new Action(SpawnProtected.RefreshConfigs));
				SpawnProtected._eventAssigned = true;
			}
			SpawnProtected.RefreshConfigs();
		}

		private static void RefreshConfigs()
		{
			SpawnProtected.ProtectedTeams.Clear();
			SpawnProtected.IsProtectionEnabled = ConfigFile.ServerConfig.GetBool("spawn_protect_enabled", false);
			SpawnProtected.CanShoot = ConfigFile.ServerConfig.GetBool("spawn_protect_can_shoot", false);
			SpawnProtected.PreventAllDamage = ConfigFile.ServerConfig.GetBool("spawn_protect_prevent_all", false);
			SpawnProtected.SpawnDuration = ConfigFile.ServerConfig.GetFloat("spawn_protect_time", 8f);
			List<int> intList = ConfigFile.ServerConfig.GetIntList("spawn_protect_team");
			if (intList.Count == 0)
			{
				SpawnProtected.ProtectedTeams.Add(Team.FoundationForces);
				SpawnProtected.ProtectedTeams.Add(Team.ChaosInsurgency);
				return;
			}
			intList.ForEach(delegate(int t)
			{
				SpawnProtected.ProtectedTeams.Add((Team)t);
			});
		}

		public static bool IsProtectionEnabled;

		public static bool CanShoot;

		public static bool PreventAllDamage;

		public static float SpawnDuration;

		public static readonly List<Team> ProtectedTeams = new List<Team>();

		private static bool _eventAssigned = false;
	}
}
