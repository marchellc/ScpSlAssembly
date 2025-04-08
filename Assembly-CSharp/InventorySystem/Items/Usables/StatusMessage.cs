using System;
using Mirror;

namespace InventorySystem.Items.Usables
{
	public struct StatusMessage : NetworkMessage
	{
		public StatusMessage(StatusMessage.StatusType status, ushort serial)
		{
			this.Status = status;
			this.ItemSerial = serial;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteByte((byte)this.Status);
			writer.WriteUShort(this.ItemSerial);
		}

		public StatusMessage.StatusType Status;

		public ushort ItemSerial;

		public enum StatusType : byte
		{
			Start,
			Cancel
		}
	}
}
