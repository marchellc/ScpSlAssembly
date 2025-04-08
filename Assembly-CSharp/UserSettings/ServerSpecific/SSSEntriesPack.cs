using System;
using Mirror;

namespace UserSettings.ServerSpecific
{
	public readonly struct SSSEntriesPack : NetworkMessage
	{
		public SSSEntriesPack(NetworkReader reader)
		{
			this.Version = reader.ReadInt();
			this.Settings = new ServerSpecificSettingBase[(int)reader.ReadByte()];
			for (int i = 0; i < this.Settings.Length; i++)
			{
				ServerSpecificSettingBase serverSpecificSettingBase = ServerSpecificSettingsSync.CreateInstance(ServerSpecificSettingsSync.GetTypeFromCode(reader.ReadByte())) as ServerSpecificSettingBase;
				serverSpecificSettingBase.DeserializeEntry(reader);
				this.Settings[i] = serverSpecificSettingBase;
			}
		}

		public SSSEntriesPack(ServerSpecificSettingBase[] settings, int version)
		{
			this.Settings = settings;
			this.Version = version;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(this.Version);
			if (this.Settings == null)
			{
				writer.WriteByte(0);
				return;
			}
			writer.WriteByte((byte)this.Settings.Length);
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in this.Settings)
			{
				writer.WriteByte(ServerSpecificSettingsSync.GetCodeFromType(serverSpecificSettingBase.GetType()));
				serverSpecificSettingBase.SerializeEntry(writer);
			}
		}

		public readonly ServerSpecificSettingBase[] Settings;

		public readonly int Version;
	}
}
