using Mirror;
using PlayerRoles;

namespace Respawning.Objectives;

public abstract class HumanObjectiveBase<T> : FactionObjectiveBase, IFootprintObjective where T : ObjectiveFootprintBase
{
	public ObjectiveFootprintBase ObjectiveFootprint { get; set; }

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this.ObjectiveFootprint.ServerWriteRpc(writer);
	}

	public override void ClientReadRpc(NetworkReader reader)
	{
		base.ClientReadRpc(reader);
		this.ObjectiveFootprint = this.ClientCreateFootprint();
		this.ObjectiveFootprint.ClientReadRpc(reader);
	}

	protected abstract T ClientCreateFootprint();

	protected override bool IsValidFaction(Faction faction)
	{
		if (faction != Faction.FoundationStaff)
		{
			return faction == Faction.FoundationEnemy;
		}
		return true;
	}
}
