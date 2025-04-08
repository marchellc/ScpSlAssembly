using System;
using CustomPlayerEffects;
using GameCore;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.MicroHID;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using UnityEngine;

namespace Achievements.Handlers
{
	public class GeneralKillsHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += GeneralKillsHandler.HandleDeath;
		}

		private static void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ExplosionDamageHandler explosionDamageHandler = handler as ExplosionDamageHandler;
			AttackerDamageHandler attackerDamageHandler;
			if (explosionDamageHandler == null)
			{
				Scp939DamageHandler scp939DamageHandler = handler as Scp939DamageHandler;
				if (scp939DamageHandler == null)
				{
					ScpDamageHandler scpDamageHandler = handler as ScpDamageHandler;
					if (scpDamageHandler == null)
					{
						MicroHidDamageHandler microHidDamageHandler = handler as MicroHidDamageHandler;
						if (microHidDamageHandler == null)
						{
							attackerDamageHandler = handler as AttackerDamageHandler;
							if (attackerDamageHandler != null)
							{
								goto IL_014F;
							}
							UniversalDamageHandler universalDamageHandler = handler as UniversalDamageHandler;
							if (universalDamageHandler == null)
							{
								return;
							}
							if (universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id && deadPlayer.inventory.CurInstance is MicroHIDItem)
							{
								GeneralKillsHandler.Send(deadPlayer, AchievementName.Overcurrent);
								return;
							}
							return;
						}
						else if (microHidDamageHandler.Attacker.Hub != null)
						{
							if (deadPlayer.IsSCP(true))
							{
								GeneralKillsHandler.Send(microHidDamageHandler.Attacker.Hub, AchievementName.MicrowaveMeal);
								return;
							}
							return;
						}
					}
					else if (scpDamageHandler.Attacker.Hub != null)
					{
						if (RoundStart.RoundStartTimer.Elapsed.TotalSeconds < 60.0)
						{
							GeneralKillsHandler.Send(scpDamageHandler.Attacker.Hub, AchievementName.AnomalouslyEfficient);
							return;
						}
						return;
					}
				}
				else if (scp939DamageHandler.Attacker.Hub != null)
				{
					GeneralKillsHandler.HandleScp939Kill(deadPlayer, scp939DamageHandler);
					return;
				}
			}
			else if (explosionDamageHandler.Attacker.Hub != null)
			{
				if (explosionDamageHandler.Attacker.Hub != deadPlayer && explosionDamageHandler.ExplosionType == ExplosionType.Grenade)
				{
					GeneralKillsHandler.Send(explosionDamageHandler.Attacker.Hub, AchievementName.FireInTheHole);
					return;
				}
				return;
			}
			attackerDamageHandler = (AttackerDamageHandler)handler;
			IL_014F:
			if (attackerDamageHandler.Attacker.Hub != null)
			{
				GeneralKillsHandler.HandleAttackerKill(deadPlayer, attackerDamageHandler);
				return;
			}
		}

		private static void HandleScp939Kill(ReferenceHub deadPlayer, Scp939DamageHandler scp939DH)
		{
			if (!deadPlayer.IsHuman())
			{
				return;
			}
			if (scp939DH.Attacker.Role != RoleTypeId.Scp939)
			{
				return;
			}
			AmnesiaVision amnesiaVision;
			if (!deadPlayer.playerEffectsController.TryGetEffect<AmnesiaVision>(out amnesiaVision))
			{
				return;
			}
			if (amnesiaVision.IsEnabled && scp939DH.Scp939DamageType == Scp939DamageType.LungeTarget)
			{
				GeneralKillsHandler.Send(scp939DH.Attacker.Hub, AchievementName.AmnesticAmbush);
			}
		}

		private static void HandleAttackerKill(ReferenceHub deadPlayer, AttackerDamageHandler aDH)
		{
			ReferenceHub hub = aDH.Attacker.Hub;
			RoleTypeId roleId = deadPlayer.GetRoleId();
			RoleTypeId roleTypeId = roleId;
			if (roleTypeId != RoleTypeId.Scp096)
			{
				if (roleTypeId == RoleTypeId.Scp0492 && hub.IsHuman() && aDH is JailbirdDamageHandler)
				{
					GeneralKillsHandler.Send(hub, AchievementName.UndeadSpaceProgram);
				}
			}
			else if ((deadPlayer.roleManager.CurrentRole as Scp096Role).IsRageState(Scp096RageState.Distressed))
			{
				GeneralKillsHandler.Send(hub, AchievementName.Pacified);
			}
			Team team = aDH.Attacker.Role.GetTeam();
			if (team != Team.Scientists)
			{
				if (team == Team.ClassD)
				{
					if (roleId == RoleTypeId.Scientist && deadPlayer.inventory.CurInstance is KeycardItem)
					{
						GeneralKillsHandler.Send(hub, AchievementName.AccessGranted);
					}
				}
			}
			else if (deadPlayer.IsSCP(true))
			{
				GeneralKillsHandler.Send(hub, AchievementName.SomethingDoneRight);
			}
			FirearmDamageHandler firearmDamageHandler = aDH as FirearmDamageHandler;
			if (firearmDamageHandler != null && firearmDamageHandler.WeaponType == ItemType.GunRevolver)
			{
				if (!deadPlayer.IsHuman() && deadPlayer.GetRoleId() != RoleTypeId.Scp0492)
				{
					return;
				}
				if (firearmDamageHandler.Hitbox != HitboxType.Headshot)
				{
					return;
				}
				if (Vector3.Distance(deadPlayer.transform.position, hub.transform.position) < 25f)
				{
					return;
				}
				GeneralKillsHandler.Send(hub, AchievementName.ArizonaRanger);
			}
		}

		private static void Send(ReferenceHub hub, AchievementName name)
		{
			if (hub != null)
			{
				AchievementHandlerBase.ServerAchieve(hub.networkIdentity.connectionToClient, name);
			}
		}

		private const int AnomalouslyEfficientTime = 60;

		private const int ArizonaRangerDistance = 25;
	}
}
