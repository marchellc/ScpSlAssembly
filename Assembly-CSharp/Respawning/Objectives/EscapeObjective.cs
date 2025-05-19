using InventorySystem.Disarming;
using PlayerRoles;

namespace Respawning.Objectives;

public class EscapeObjective : HumanObjectiveBase<EscapeObjectiveFootprint>
{
	private const float EscapeInfluence = 5f;

	private const float CuffedEscapeInfluence = 5f;

	private const float ClassDEscapeTime = -20f;

	private const float ClassDEscapeInfluence = 5f;

	private const float ScientistEscapeTime = -30f;

	private const float ScientistEscapeInfluence = 5f;

	private const float CuffedClassDEscapeTime = -10f;

	private const float CuffedClassDEscapeInfluence = 5f;

	private const float CuffedScientistEscapeTime = -20f;

	private const float CuffedScientistEscapeInfluence = 5f;

	protected override EscapeObjectiveFootprint ClientCreateFootprint()
	{
		return new EscapeObjectiveFootprint();
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		PlayerRoleManager.OnServerRoleSet += OnServerRoleSet;
	}

	private void OnServerRoleSet(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
	{
		if (reason != RoleChangeReason.Escaped)
		{
			return;
		}
		bool flag = hub.inventory.IsDisarmed();
		Faction faction = hub.GetFaction();
		Faction faction2 = (flag ? GetOpposingFaction(faction) : faction);
		if (faction2 != Faction.Unclassified)
		{
			float num = 0f;
			float num2 = 0f;
			switch (hub.roleManager.CurrentRole.RoleTypeId)
			{
			case RoleTypeId.ClassD:
				num = (flag ? (-10f) : (-20f));
				num2 = (flag ? 5f : 5f);
				break;
			case RoleTypeId.Scientist:
				num = (flag ? (-20f) : (-30f));
				num2 = (flag ? 5f : 5f);
				break;
			}
			GrantInfluence(faction2, num2);
			ReduceTimer(faction2, num);
			base.ObjectiveFootprint = new EscapeObjectiveFootprint
			{
				AchievingPlayer = new ObjectiveHubFootprint(hub, newRole),
				InfluenceReward = num2,
				TimeReward = num
			};
			ServerSendUpdate();
		}
	}

	private Faction GetOpposingFaction(Faction faction)
	{
		return faction switch
		{
			Faction.FoundationStaff => Faction.FoundationEnemy, 
			Faction.FoundationEnemy => Faction.FoundationStaff, 
			_ => Faction.Unclassified, 
		};
	}
}
