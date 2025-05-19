using Mirror;

namespace InventorySystem.Items.Pickups;

public static class PickupSyncInfoSerializer
{
	public static void WritePickupSyncInfo(this NetworkWriter writer, PickupSyncInfo value)
	{
		writer.WriteSByte((sbyte)value.ItemId);
		writer.WriteUShort(value.Serial);
		writer.WriteFloat(value.WeightKg);
		writer.WriteByte(value.SyncedFlags);
	}

	public static PickupSyncInfo ReadPickupSyncInfo(this NetworkReader reader)
	{
		PickupSyncInfo result = default(PickupSyncInfo);
		result.ItemId = (ItemType)reader.ReadSByte();
		result.Serial = reader.ReadUShort();
		result.WeightKg = reader.ReadFloat();
		result.SyncedFlags = reader.ReadByte();
		return result;
	}
}
