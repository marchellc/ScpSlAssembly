using System;
using Mirror;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public readonly struct ReflexSightSyncData
	{
		public ReflexSightSyncData(int colorHue, int colorBrightness, int size, int iconIndex)
		{
			this._colorHue = colorHue;
			this._colorBrightness = colorBrightness;
			this._size = size;
			this._iconIndex = iconIndex;
		}

		public ReflexSightSyncData(ReflexSightAttachment attachment)
		{
			this._colorHue = attachment.CurColorIndex;
			this._colorBrightness = attachment.CurBrightnessIndex;
			this._size = attachment.CurSizeIndex;
			this._iconIndex = attachment.CurTextureIndex;
		}

		public ReflexSightSyncData(NetworkReader reader)
		{
			ushort num = reader.ReadUShort();
			this._colorHue = (num & 61440) >> 12;
			this._colorBrightness = (num & 3840) >> 8;
			this._size = (num & 240) >> 4;
			this._iconIndex = (int)(num & 15);
		}

		public void Write(NetworkWriter writer)
		{
			int num = this._colorHue << 12;
			int num2 = this._colorBrightness << 8;
			int num3 = this._size << 4;
			int iconIndex = this._iconIndex;
			writer.WriteUShort((ushort)(num + num2 + num3 + iconIndex));
		}

		public void Apply(ReflexSightAttachment att)
		{
			att.SetValues(this._iconIndex, this._colorHue, this._size, this._colorBrightness);
		}

		private readonly int _colorHue;

		private readonly int _colorBrightness;

		private readonly int _size;

		private readonly int _iconIndex;
	}
}
