using Footprinting;
using MapGeneration.Distributors;
using PlayerRoles;
using Utils.NonAllocLINQ;

namespace Respawning.Objectives;

public class GeneratorActivatedObjective : HumanObjectiveBase<GeneratorObjectiveFootprint>
{
	private const float GeneratorActivatedTimer = -10f;

	private const float GeneratorActivatedInfluence = 3f;

	protected override GeneratorObjectiveFootprint ClientCreateFootprint()
	{
		return new GeneratorObjectiveFootprint();
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		Scp079Generator.OnGeneratorEngaged += OnGeneratorEngaged;
	}

	private void OnGeneratorEngaged(Scp079Generator generator, Footprint footprint)
	{
		bool num = ReferenceHub.AllHubs.Any((ReferenceHub hub) => hub.GetRoleId() == RoleTypeId.Scp079);
		Faction faction = footprint.Role.GetFaction();
		base.GrantInfluence(faction, 3f);
		if (num)
		{
			base.ReduceTimer(faction, -10f);
		}
		base.ObjectiveFootprint = new GeneratorObjectiveFootprint
		{
			InfluenceReward = 3f,
			TimeReward = -10f,
			AchievingPlayer = new ObjectiveHubFootprint(footprint)
		};
		base.ServerSendUpdate();
	}
}
