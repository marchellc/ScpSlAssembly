using Mirror;

namespace InventorySystem.Items.Firearms.Attachments;

public struct AttachmentsSetupPreference : NetworkMessage
{
	public ItemType Weapon;

	public uint AttachmentsCode;

	public AttachmentsSetupPreference(NetworkReader reader)
	{
		this.Weapon = (ItemType)reader.ReadByte();
		this.AttachmentsCode = reader.ReadUInt();
	}

	public readonly void Serialize(NetworkWriter writer)
	{
		writer.WriteByte((byte)this.Weapon);
		writer.WriteUInt(this.AttachmentsCode);
	}
}
