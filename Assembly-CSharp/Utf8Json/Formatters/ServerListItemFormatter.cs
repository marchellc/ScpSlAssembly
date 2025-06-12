using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters;

public sealed class ServerListItemFormatter : IJsonFormatter<ServerListItem>, IJsonFormatter
{
	private readonly AutomataDictionary ____keyMapping;

	private readonly byte[][] ____stringByteKeys;

	public ServerListItemFormatter()
	{
		this.____keyMapping = new AutomataDictionary
		{
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("serverId"),
				0
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("ip"),
				1
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("port"),
				2
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("players"),
				3
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("info"),
				4
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("pastebin"),
				5
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("version"),
				6
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("friendlyFire"),
				7
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("modded"),
				8
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("whitelist"),
				9
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("officialCode"),
				10
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("NameFilterPoints"),
				11
			}
		};
		this.____stringByteKeys = new byte[12][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("serverId"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("ip"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("port"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("players"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("info"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("pastebin"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("version"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("friendlyFire"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("modded"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("whitelist"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("officialCode"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("NameFilterPoints")
		};
	}

	public void Serialize(ref JsonWriter writer, ServerListItem value, IJsonFormatterResolver formatterResolver)
	{
		writer.WriteRaw(this.____stringByteKeys[0]);
		writer.WriteUInt32(value.serverId);
		writer.WriteRaw(this.____stringByteKeys[1]);
		writer.WriteString(value.ip);
		writer.WriteRaw(this.____stringByteKeys[2]);
		writer.WriteUInt16(value.port);
		writer.WriteRaw(this.____stringByteKeys[3]);
		writer.WriteString(value.players);
		writer.WriteRaw(this.____stringByteKeys[4]);
		writer.WriteString(value.info);
		writer.WriteRaw(this.____stringByteKeys[5]);
		writer.WriteString(value.pastebin);
		writer.WriteRaw(this.____stringByteKeys[6]);
		writer.WriteString(value.version);
		writer.WriteRaw(this.____stringByteKeys[7]);
		writer.WriteBoolean(value.friendlyFire);
		writer.WriteRaw(this.____stringByteKeys[8]);
		writer.WriteBoolean(value.modded);
		writer.WriteRaw(this.____stringByteKeys[9]);
		writer.WriteBoolean(value.whitelist);
		writer.WriteRaw(this.____stringByteKeys[10]);
		writer.WriteByte(value.officialCode);
		writer.WriteRaw(this.____stringByteKeys[11]);
		writer.WriteInt32(value.NameFilterPoints);
		writer.WriteEndObject();
	}

	public ServerListItem Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (reader.ReadIsNull())
		{
			throw new InvalidOperationException("typecode is null, struct not supported");
		}
		uint serverId = 0u;
		string ip = null;
		ushort port = 0;
		string players = null;
		string info = null;
		string pastebin = null;
		string version = null;
		bool friendlyFire = false;
		bool modded = false;
		bool whitelist = false;
		byte officialCode = 0;
		int nameFilterPoints = 0;
		bool flag = false;
		int count = 0;
		reader.ReadIsBeginObjectWithVerify();
		while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
		{
			ArraySegment<byte> key = reader.ReadPropertyNameSegmentRaw();
			if (!this.____keyMapping.TryGetValueSafe(key, out var value))
			{
				reader.ReadNextBlock();
				continue;
			}
			switch (value)
			{
			case 0:
				serverId = reader.ReadUInt32();
				break;
			case 1:
				ip = reader.ReadString();
				break;
			case 2:
				port = reader.ReadUInt16();
				break;
			case 3:
				players = reader.ReadString();
				break;
			case 4:
				info = reader.ReadString();
				break;
			case 5:
				pastebin = reader.ReadString();
				break;
			case 6:
				version = reader.ReadString();
				break;
			case 7:
				friendlyFire = reader.ReadBoolean();
				break;
			case 8:
				modded = reader.ReadBoolean();
				break;
			case 9:
				whitelist = reader.ReadBoolean();
				break;
			case 10:
				officialCode = reader.ReadByte();
				break;
			case 11:
				nameFilterPoints = reader.ReadInt32();
				flag = true;
				break;
			default:
				reader.ReadNextBlock();
				break;
			}
		}
		ServerListItem result = new ServerListItem(serverId, ip, port, players, info, pastebin, version, friendlyFire, modded, whitelist, officialCode);
		if (flag)
		{
			result.NameFilterPoints = nameFilterPoints;
		}
		return result;
	}
}
