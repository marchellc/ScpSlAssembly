using Mirror;

namespace UserSettings.ServerSpecific;

public readonly struct SSSUserStatusReport : NetworkMessage
{
	public readonly int Version;

	public readonly bool TabOpen;

	public SSSUserStatusReport(NetworkReader reader)
	{
		this.Version = reader.ReadInt();
		this.TabOpen = reader.ReadBool();
	}

	public SSSUserStatusReport(int ver, bool tabOpen)
	{
		this.Version = ver;
		this.TabOpen = tabOpen;
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteInt(this.Version);
		writer.WriteBool(this.TabOpen);
	}
}
