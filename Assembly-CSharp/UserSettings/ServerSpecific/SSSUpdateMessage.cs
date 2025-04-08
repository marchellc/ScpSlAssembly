using System;
using System.Collections.Generic;
using Mirror;
using NorthwoodLib.Pools;

namespace UserSettings.ServerSpecific
{
	public readonly struct SSSUpdateMessage : NetworkMessage
	{
		public SSSUpdateMessage(NetworkReader reader)
		{
			this.Id = reader.ReadInt();
			this.TypeCode = reader.ReadByte();
			int num = reader.ReadInt();
			this.DeserializedPooledPayload = ListPool<byte>.Shared.Rent(reader.ReadBytesSegment(num));
			this.ServersidePayloadWriter = null;
		}

		public SSSUpdateMessage(ServerSpecificSettingBase setting, Action<NetworkWriter> writerFunc)
		{
			this.Id = setting.SettingId;
			this.TypeCode = ServerSpecificSettingsSync.GetCodeFromType(setting.GetType());
			this.DeserializedPooledPayload = null;
			this.ServersidePayloadWriter = writerFunc;
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(this.Id);
			writer.WriteByte(this.TypeCode);
			using (NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get())
			{
				Action<NetworkWriter> serversidePayloadWriter = this.ServersidePayloadWriter;
				if (serversidePayloadWriter != null)
				{
					serversidePayloadWriter(networkWriterPooled);
				}
				writer.WriteArraySegment(networkWriterPooled.ToArraySegment());
			}
		}

		public readonly int Id;

		public readonly byte TypeCode;

		public readonly List<byte> DeserializedPooledPayload;

		public readonly Action<NetworkWriter> ServersidePayloadWriter;
	}
}
