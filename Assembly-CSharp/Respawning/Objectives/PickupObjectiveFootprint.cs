using System.Text;
using InventorySystem.Items;
using Mirror;

namespace Respawning.Objectives;

public class PickupObjectiveFootprint : ObjectiveFootprintBase
{
	public ItemType PickupType { get; set; }

	protected override FootprintsTranslation TargetTranslation => FootprintsTranslation.ItemPickupObjective;

	public override void ClientReadRpc(NetworkReader reader)
	{
		base.ClientReadRpc(reader);
		this.PickupType = (ItemType)reader.ReadByte();
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this.PickupType);
	}

	public override StringBuilder ClientCompletionText(StringBuilder builder)
	{
		base.ClientCompletionText(builder);
		builder.Replace("%itemName%", this.PickupType.GetName());
		return builder;
	}
}
