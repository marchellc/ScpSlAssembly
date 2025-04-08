using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ServerListItemFormatter : IJsonFormatter<ServerListItem>, IJsonFormatter
	{
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
			this.____stringByteKeys = new byte[][]
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
			uint num = 0U;
			string text = null;
			ushort num2 = 0;
			string text2 = null;
			string text3 = null;
			string text4 = null;
			string text5 = null;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			byte b = 0;
			int num3 = 0;
			bool flag4 = false;
			int num4 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num4))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num5;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num5))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num5)
					{
					case 0:
						num = reader.ReadUInt32();
						break;
					case 1:
						text = reader.ReadString();
						break;
					case 2:
						num2 = reader.ReadUInt16();
						break;
					case 3:
						text2 = reader.ReadString();
						break;
					case 4:
						text3 = reader.ReadString();
						break;
					case 5:
						text4 = reader.ReadString();
						break;
					case 6:
						text5 = reader.ReadString();
						break;
					case 7:
						flag = reader.ReadBoolean();
						break;
					case 8:
						flag2 = reader.ReadBoolean();
						break;
					case 9:
						flag3 = reader.ReadBoolean();
						break;
					case 10:
						b = reader.ReadByte();
						break;
					case 11:
						num3 = reader.ReadInt32();
						flag4 = true;
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			ServerListItem serverListItem = new ServerListItem(num, text, num2, text2, text3, text4, text5, flag, flag2, flag3, b);
			if (flag4)
			{
				serverListItem.NameFilterPoints = num3;
			}
			return serverListItem;
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
