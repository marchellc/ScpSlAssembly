using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Respawning.Objectives
{
	public class HumanDamageObjective : HumanObjectiveBase<DamageObjectiveFootprint>
	{
		protected override DamageObjectiveFootprint ClientCreateFootprint()
		{
			return new DamageObjectiveFootprint();
		}

		protected override void OnInstanceCreated()
		{
			base.OnInstanceCreated();
			PlayerStats.OnAnyPlayerDamaged += this.OnPlayerDamaged;
			PlayerRoleManager.OnServerRoleSet += this.OnServerRoleSet;
		}

		protected override void OnInstanceReset()
		{
			base.OnInstanceReset();
			HumanDamageObjective.HealthTracker.Clear();
		}

		private void OnPlayerDamaged(ReferenceHub victim, DamageHandlerBase dhb)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (!victim.IsSCP(false))
			{
				return;
			}
			AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			Faction faction = attackerDamageHandler.Attacker.Role.GetFaction();
			if (!this.IsValidFaction(faction))
			{
				return;
			}
			HealthStat healthStat;
			if (!victim.playerStats.TryGetModule<HealthStat>(out healthStat))
			{
				return;
			}
			int num;
			if (!HumanDamageObjective.HealthTracker.TryGetValue(victim, out num))
			{
				num = 0;
			}
			int num2 = Mathf.FloorToInt((1f - healthStat.NormalizedValue) * 100f / 20f);
			if (num2 >= HumanDamageObjective.MaxThresholdAmount)
			{
				return;
			}
			if (num2 <= num)
			{
				return;
			}
			HumanDamageObjective.HealthTracker[victim] = num2;
			int num3 = num2 - num;
			float num4 = -4f * (float)num3;
			float num5 = 4f * (float)num3;
			base.ReduceTimer(faction, num4);
			base.GrantInfluence(faction, num5);
			base.ObjectiveFootprint = new DamageObjectiveFootprint
			{
				InfluenceReward = num5,
				TimeReward = num4,
				AchievingPlayer = new ObjectiveHubFootprint(attackerDamageHandler.Attacker),
				VictimFootprint = new ObjectiveHubFootprint(victim, RoleTypeId.None)
			};
			base.ServerSendUpdate();
		}

		private void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			HumanDamageObjective.HealthTracker.Remove(userHub);
		}

		private const float TimerReward = -4f;

		private const float InfluenceReward = 4f;

		private const float HealthThresholdPercentage = 20f;

		private const float MaxPercentage = 100f;

		public static Dictionary<ReferenceHub, int> HealthTracker = new Dictionary<ReferenceHub, int>();

		private static readonly int MaxThresholdAmount = Mathf.FloorToInt(5f);
	}
}
