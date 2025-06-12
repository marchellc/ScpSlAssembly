using Mirror;

namespace InventorySystem.Items.Usables;

public struct ItemCooldownMessage : NetworkMessage
{
	public ushort ItemSerial;

	public float RemainingTime;

	public ItemCooldownMessage(ushort serial, float remainingTime)
	{
		this.ItemSerial = serial;
		this.RemainingTime = remainingTime;
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteUShort(this.ItemSerial);
		writer.WriteFloat(this.RemainingTime);
	}
}
