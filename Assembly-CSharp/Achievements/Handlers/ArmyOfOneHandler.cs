using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class ArmyOfOneHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += ArmyOfOneHandler.OnAnyPlayerDied;
			PlayerRoleManager.OnServerRoleSet += ArmyOfOneHandler.OnServerRoleSet;
		}

		internal override void OnRoundStarted()
		{
			ArmyOfOneHandler.Kills.Clear();
		}

		private static void OnServerRoleSet(ReferenceHub hub, RoleTypeId roleTypeId, RoleChangeReason changeReason)
		{
			if (changeReason == RoleChangeReason.Escaped)
			{
				return;
			}
			ArmyOfOneHandler.Kills.Remove(hub);
		}

		private static void OnAnyPlayerDied(ReferenceHub victim, DamageHandlerBase handler)
		{
			if (NetworkServer.active)
			{
				AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
				if (attackerDamageHandler != null)
				{
					ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
					if (hub == null || !hub.IsHuman())
					{
						return;
					}
					if (!HitboxIdentity.IsEnemy(attackerDamageHandler.Attacker.Role, victim.GetRoleId()))
					{
						return;
					}
					ItemType itemType = ArmyOfOneHandler.GetItemType(handler);
					if (itemType == ItemType.None)
					{
						return;
					}
					List<ItemType> list;
					if (!ArmyOfOneHandler.Kills.TryGetValue(hub, out list))
					{
						ArmyOfOneHandler.Kills[hub] = new List<ItemType> { itemType };
						return;
					}
					if (list.Contains(itemType))
					{
						return;
					}
					list.Add(itemType);
					ArmyOfOneHandler.Kills[hub] = list;
					if (list.Count < 4)
					{
						return;
					}
					AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.ArmyOfOne);
					return;
				}
			}
		}

		private static ItemType GetItemType(DamageHandlerBase handler)
		{
			FirearmDamageHandler firearmDamageHandler = handler as FirearmDamageHandler;
			if (firearmDamageHandler != null)
			{
				return firearmDamageHandler.WeaponType;
			}
			if (handler is DisruptorDamageHandler)
			{
				return ItemType.ParticleDisruptor;
			}
			if (handler is JailbirdDamageHandler)
			{
				return ItemType.Jailbird;
			}
			if (handler is MicroHidDamageHandler)
			{
				return ItemType.MicroHID;
			}
			ExplosionDamageHandler explosionDamageHandler = handler as ExplosionDamageHandler;
			if (explosionDamageHandler == null)
			{
				return ItemType.None;
			}
			return ArmyOfOneHandler.ExplosionToItemType(explosionDamageHandler.ExplosionType);
		}

		private static ItemType ExplosionToItemType(ExplosionType explosionType)
		{
			if (explosionType == ExplosionType.Grenade)
			{
				return ItemType.GrenadeHE;
			}
			if (explosionType == ExplosionType.Disruptor)
			{
				return ItemType.ParticleDisruptor;
			}
			if (explosionType != ExplosionType.Jailbird)
			{
				return ItemType.None;
			}
			return ItemType.Jailbird;
		}

		private const int KillsNeeded = 4;

		private static readonly Dictionary<ReferenceHub, List<ItemType>> Kills = new Dictionary<ReferenceHub, List<ItemType>>();
	}
}
