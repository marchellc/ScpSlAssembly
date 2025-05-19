namespace InventorySystem.Items;

public interface ISoundEmittingItem
{
	bool ServerTryGetSoundEmissionRange(out float range);
}
