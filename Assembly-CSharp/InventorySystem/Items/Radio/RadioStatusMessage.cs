using Mirror;

namespace InventorySystem.Items.Radio;

public struct RadioStatusMessage : NetworkMessage
{
	public readonly RadioMessages.RadioRangeLevel Range;

	public readonly byte Battery;

	public readonly uint Owner;

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteSByte((sbyte)this.Range);
		writer.WriteByte(this.Battery);
		writer.WriteUInt(this.Owner);
	}

	public RadioStatusMessage(NetworkReader reader)
	{
		this.Range = (RadioMessages.RadioRangeLevel)reader.ReadSByte();
		this.Battery = reader.ReadByte();
		this.Owner = reader.ReadUInt();
	}

	public RadioStatusMessage(RadioItem radio)
	{
		this.Range = radio.RangeLevel;
		this.Battery = radio.BatteryPercent;
		this.Owner = radio.Owner.netId;
	}
}
