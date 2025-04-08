using System;
using Mirror;

namespace InventorySystem.Items.Autosync
{
	public readonly struct AutosyncMessage : NetworkMessage
	{
		public AutosyncMessage(NetworkWriter writer, ItemIdentifier itemId)
		{
			this._serial = itemId.SerialNumber;
			this._itemType = itemId.TypeId;
			int position = writer.Position;
			writer.WriteByte((byte)Math.Min(position, 255));
			if (position >= 255)
			{
				writer.WriteUShort((ushort)(position - 255));
			}
			this._bytesWritten = position;
			Array.Copy(writer.buffer, AutosyncMessage.Buffer, this._bytesWritten);
		}

		public override string ToString()
		{
			return string.Format("{0} (Item={1} Serial={2} Length={3} Payload={4})", new object[]
			{
				"AutosyncMessage",
				this._itemType,
				this._serial,
				this._bytesWritten,
				AutosyncMessage.Reader
			});
		}

		internal AutosyncMessage(NetworkReader reader)
		{
			this._serial = reader.ReadUShort();
			this._itemType = (ItemType)reader.ReadByte();
			this._bytesWritten = (int)reader.ReadByte();
			if (this._bytesWritten == 255)
			{
				this._bytesWritten += (int)reader.ReadUShort();
			}
			reader.ReadBytes(AutosyncMessage.Buffer, this._bytesWritten);
		}

		internal void Serialize(NetworkWriter writer)
		{
			writer.WriteUShort(this._serial);
			writer.WriteByte((byte)this._itemType);
			if (this._bytesWritten < 255)
			{
				writer.WriteByte((byte)this._bytesWritten);
			}
			else
			{
				writer.WriteByte(byte.MaxValue);
				writer.WriteUShort((ushort)(this._bytesWritten - 255));
			}
			writer.WriteBytes(AutosyncMessage.Buffer, 0, this._bytesWritten);
		}

		internal void ProcessCmd(ReferenceHub sender)
		{
			ItemBase itemBase;
			if (!sender.inventory.UserInventory.Items.TryGetValue(this._serial, out itemBase))
			{
				return;
			}
			AutosyncItem autosyncItem = itemBase as AutosyncItem;
			if (autosyncItem == null || autosyncItem.ItemTypeId != this._itemType)
			{
				return;
			}
			this.ResetReader();
			autosyncItem.ServerProcessCmd(AutosyncMessage.Reader);
		}

		internal void ProcessRpc()
		{
			AutosyncItem autosyncItem;
			if (InventoryItemLoader.TryGetItem<AutosyncItem>(this._itemType, out autosyncItem))
			{
				this.ResetReader();
				autosyncItem.ClientProcessRpcTemplate(AutosyncMessage.Reader, this._serial);
			}
			if (this._serial == 0)
			{
				return;
			}
			foreach (AutosyncItem autosyncItem2 in AutosyncItem.Instances)
			{
				if (autosyncItem2.ItemSerial == this._serial && autosyncItem2.ItemTypeId == this._itemType)
				{
					this.ResetReader();
					autosyncItem2.ClientProcessRpcInstance(AutosyncMessage.Reader);
				}
			}
		}

		private void ResetReader()
		{
			AutosyncMessage.Reader.buffer = new ArraySegment<byte>(AutosyncMessage.Buffer, 0, this._bytesWritten);
			AutosyncMessage.Reader.Position = 0;
		}

		private static readonly byte[] Buffer = new byte[65790];

		private static readonly NetworkReader Reader = new NetworkReader(AutosyncMessage.Buffer);

		private readonly int _bytesWritten;

		private readonly ushort _serial;

		private readonly ItemType _itemType;
	}
}
