using Mirror;

namespace UserSettings.ServerSpecific;

public readonly struct SSSEntriesPack : NetworkMessage
{
	public readonly ServerSpecificSettingBase[] Settings;

	public readonly int Version;

	public SSSEntriesPack(NetworkReader reader)
	{
		Version = reader.ReadInt();
		Settings = new ServerSpecificSettingBase[reader.ReadByte()];
		for (int i = 0; i < Settings.Length; i++)
		{
			ServerSpecificSettingBase serverSpecificSettingBase = ServerSpecificSettingsSync.CreateInstance(ServerSpecificSettingsSync.GetTypeFromCode(reader.ReadByte())) as ServerSpecificSettingBase;
			serverSpecificSettingBase.DeserializeEntry(reader);
			Settings[i] = serverSpecificSettingBase;
		}
	}

	public SSSEntriesPack(ServerSpecificSettingBase[] settings, int version)
	{
		Settings = settings;
		Version = version;
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteInt(Version);
		if (Settings == null)
		{
			writer.WriteByte(0);
			return;
		}
		writer.WriteByte((byte)Settings.Length);
		ServerSpecificSettingBase[] settings = Settings;
		foreach (ServerSpecificSettingBase serverSpecificSettingBase in settings)
		{
			writer.WriteByte(ServerSpecificSettingsSync.GetCodeFromType(serverSpecificSettingBase.GetType()));
			serverSpecificSettingBase.SerializeEntry(writer);
		}
	}
}
