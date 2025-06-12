namespace InventorySystem.Items.Firearms.ShotEvents;

public abstract class ShotEvent
{
	public readonly ItemIdentifier ItemId;

	public ShotEvent(ItemIdentifier shotFirearm)
	{
		this.ItemId = shotFirearm;
	}
}
