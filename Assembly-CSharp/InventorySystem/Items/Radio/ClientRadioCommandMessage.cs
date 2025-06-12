using Mirror;

namespace InventorySystem.Items.Radio;

public struct ClientRadioCommandMessage : NetworkMessage
{
	public RadioMessages.RadioCommand Command;

	public ClientRadioCommandMessage(RadioMessages.RadioCommand cmd)
	{
		this.Command = cmd;
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteByte((byte)this.Command);
	}

	public ClientRadioCommandMessage(NetworkReader reader)
	{
		this.Command = (RadioMessages.RadioCommand)reader.ReadByte();
	}
}
