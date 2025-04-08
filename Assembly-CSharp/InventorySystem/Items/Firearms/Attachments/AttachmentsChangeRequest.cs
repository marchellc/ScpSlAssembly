using System;
using Mirror;

namespace InventorySystem.Items.Firearms.Attachments
{
	public struct AttachmentsChangeRequest : NetworkMessage
	{
		public AttachmentsChangeRequest(NetworkReader reader)
		{
			this.WeaponSerial = reader.ReadUShort();
			this.AttachmentsCode = reader.ReadUInt();
		}

		public readonly void Serialize(NetworkWriter writer)
		{
			writer.WriteUShort(this.WeaponSerial);
			writer.WriteUInt(this.AttachmentsCode);
		}

		public ushort WeaponSerial;

		public uint AttachmentsCode;
	}
}
