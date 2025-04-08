using System;
using Footprinting;
using PlayerRoles;
using PlayerStatsSystem;

namespace Respawning.Objectives
{
	public class HumanKillObjective : HumanObjectiveBase<KillObjectiveFootprint>
	{
		protected override KillObjectiveFootprint ClientCreateFootprint()
		{
			return new KillObjectiveFootprint();
		}

		protected override void OnInstanceCreated()
		{
			base.OnInstanceCreated();
			PlayerStats.OnAnyPlayerDied += this.OnKill;
		}

		private void OnKill(ReferenceHub victim, DamageHandlerBase dhb)
		{
			AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			Footprint attacker = attackerDamageHandler.Attacker;
			if (attacker.Hub == null)
			{
				return;
			}
			RoleTypeId role = attacker.Role;
			Faction faction = role.GetFaction();
			if (!this.IsValidFaction(faction) || !this.IsValidEnemy(role, victim))
			{
				return;
			}
			bool flag = victim.IsSCP(true);
			float num = (flag ? (-20f) : (-2f));
			float num2 = (flag ? 15f : 0f);
			if (flag)
			{
				base.GrantInfluence(faction, num2);
			}
			base.ReduceTimer(faction, num);
			base.ObjectiveFootprint = new KillObjectiveFootprint
			{
				InfluenceReward = num2,
				TimeReward = num,
				AchievingPlayer = new ObjectiveHubFootprint(attacker),
				VictimFootprint = new ObjectiveHubFootprint(victim, RoleTypeId.None)
			};
			base.ServerSendUpdate();
		}

		private bool IsValidEnemy(RoleTypeId attacker, ReferenceHub victim)
		{
			PlayerRoleBase currentRole = victim.roleManager.CurrentRole;
			Team team = currentRole.Team;
			if (team != Team.SCPs)
			{
				if (team - Team.FoundationForces <= 1)
				{
					goto IL_002D;
				}
			}
			else if (!currentRole.RoleTypeId.IsZombie())
			{
				goto IL_002D;
			}
			return false;
			IL_002D:
			return HitboxIdentity.IsEnemy(attacker, victim.GetRoleId());
		}

		private const float MilitantKillTimer = -2f;

		private const float ScpKillInfluence = 15f;

		private const float ScpKillTimer = -20f;
	}
}
