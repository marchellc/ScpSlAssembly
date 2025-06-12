using System;
using Mirror;

namespace UserSettings.ServerSpecific;

public readonly struct SSSClientResponse : NetworkMessage
{
	public readonly Type SettingType;

	public readonly int Id;

	public readonly byte[] Payload;

	public SSSClientResponse(NetworkReader reader)
	{
		this.SettingType = ServerSpecificSettingsSync.GetTypeFromCode(reader.ReadByte());
		this.Id = reader.ReadInt();
		int count = reader.ReadInt();
		this.Payload = reader.ReadBytes(count);
	}

	public SSSClientResponse(ServerSpecificSettingBase modifiedSetting)
	{
		this.SettingType = modifiedSetting.GetType();
		this.Id = modifiedSetting.SettingId;
		using NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		modifiedSetting.SerializeValue(networkWriterPooled);
		this.Payload = networkWriterPooled.ToArray();
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteByte(ServerSpecificSettingsSync.GetCodeFromType(this.SettingType));
		writer.WriteInt(this.Id);
		writer.WriteInt(this.Payload.Length);
		writer.WriteBytes(this.Payload, 0, this.Payload.Length);
	}
}
