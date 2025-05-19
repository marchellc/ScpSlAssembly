using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Respawning.Objectives;

public class HumanDamageObjective : HumanObjectiveBase<DamageObjectiveFootprint>
{
	private const float TimerReward = -2f;

	private const float InfluenceReward = 1f;

	private const float HealthThresholdPercentage = 10f;

	private const float MaxPercentage = 100f;

	public static Dictionary<ReferenceHub, int> HealthTracker = new Dictionary<ReferenceHub, int>();

	private static readonly int MaxThresholdAmount = Mathf.FloorToInt(10f);

	protected override DamageObjectiveFootprint ClientCreateFootprint()
	{
		return new DamageObjectiveFootprint();
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		PlayerStats.OnAnyPlayerDamaged += OnPlayerDamaged;
		PlayerRoleManager.OnServerRoleSet += OnServerRoleSet;
	}

	protected override void OnInstanceReset()
	{
		base.OnInstanceReset();
		HealthTracker.Clear();
	}

	private void OnPlayerDamaged(ReferenceHub victim, DamageHandlerBase dhb)
	{
		if (!NetworkServer.active || !victim.IsSCP(includeZombies: false) || !(dhb is AttackerDamageHandler attackerDamageHandler))
		{
			return;
		}
		Faction faction = attackerDamageHandler.Attacker.Role.GetFaction();
		if (IsValidFaction(faction) && victim.playerStats.TryGetModule<HealthStat>(out var module))
		{
			if (!HealthTracker.TryGetValue(victim, out var value))
			{
				value = 0;
			}
			int num = Mathf.FloorToInt((1f - module.NormalizedValue) * 100f / 10f);
			if (num < MaxThresholdAmount && num > value)
			{
				HealthTracker[victim] = num;
				int num2 = num - value;
				float num3 = -2f * (float)num2;
				float num4 = 1f * (float)num2;
				ReduceTimer(faction, num3);
				GrantInfluence(faction, num4);
				base.ObjectiveFootprint = new DamageObjectiveFootprint
				{
					InfluenceReward = num4,
					TimeReward = num3,
					AchievingPlayer = new ObjectiveHubFootprint(attackerDamageHandler.Attacker),
					VictimFootprint = new ObjectiveHubFootprint(victim)
				};
				ServerSendUpdate();
			}
		}
	}

	private void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
	{
		if (NetworkServer.active)
		{
			HealthTracker.Remove(userHub);
		}
	}
}
