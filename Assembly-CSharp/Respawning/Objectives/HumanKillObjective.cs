using InventorySystem.Items;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerStatsSystem;

namespace Respawning.Objectives;

public class HumanKillObjective : HumanObjectiveBase<KillObjectiveFootprint>
{
	private const float MilitantKillInfluence = 1f;

	private const float MilitantKillTimer = -4f;

	private const float ScpKillInfluence = 15f;

	private const float ScpExperimentalWeaponKillInfluenceBonus = 5f;

	private const float ScpKillTimer = -10f;

	private const float ZombieKillInfluence = 0f;

	private const float ZombieKillTime = -5f;

	protected override KillObjectiveFootprint ClientCreateFootprint()
	{
		return new KillObjectiveFootprint();
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		PlayerStats.OnAnyPlayerDied += OnKill;
	}

	private void OnKill(ReferenceHub victim, DamageHandlerBase dhb)
	{
		if (!(dhb is AttackerDamageHandler { Attacker: var attacker } attackerDamageHandler) || attacker.Hub == null)
		{
			return;
		}
		RoleTypeId role = attacker.Role;
		Faction faction = role.GetFaction();
		if (IsValidFaction(faction) && IsValidEnemy(role, victim))
		{
			bool num = victim.IsSCP(includeZombies: false);
			bool flag = victim.roleManager.CurrentRole.RoleTypeId == RoleTypeId.Scp0492;
			float num2;
			float num3;
			if (num)
			{
				num2 = -10f;
				num3 = 15f;
			}
			else if (flag)
			{
				num2 = -5f;
				num3 = 0f;
			}
			else
			{
				num2 = -4f;
				num3 = 1f;
			}
			if (num && (attackerDamageHandler is MicroHidDamageHandler || attackerDamageHandler is DisruptorDamageHandler || attackerDamageHandler is JailbirdDamageHandler))
			{
				num3 += 5f;
			}
			GrantInfluence(faction, num3);
			ReduceTimer(faction, num2);
			base.ObjectiveFootprint = new KillObjectiveFootprint
			{
				InfluenceReward = num3,
				TimeReward = num2,
				AchievingPlayer = new ObjectiveHubFootprint(attacker),
				VictimFootprint = new ObjectiveHubFootprint(victim)
			};
			ServerSendUpdate();
		}
	}

	private bool IsValidEnemy(RoleTypeId attacker, ReferenceHub victim)
	{
		PlayerRoleBase currentRole = victim.roleManager.CurrentRole;
		switch (currentRole.Team)
		{
		case Team.Scientists:
		case Team.ClassD:
		{
			ItemBase curInstance = victim.inventory.CurInstance;
			if (!(curInstance == null))
			{
				ItemCategory category = curInstance.Category;
				if (category == ItemCategory.Firearm || category == ItemCategory.SpecialWeapon || category == ItemCategory.Grenade)
				{
					break;
				}
			}
			return false;
		}
		case Team.SCPs:
			if (currentRole.RoleTypeId == RoleTypeId.Scp0492 && Scp049ResurrectAbility.GetResurrectionsNumber(victim) > 1)
			{
				return false;
			}
			break;
		default:
			return false;
		case Team.FoundationForces:
		case Team.ChaosInsurgency:
			break;
		}
		return HitboxIdentity.IsEnemy(attacker, victim.GetRoleId());
	}
}
