using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NewsListItemFormatter : IJsonFormatter<NewsListItem>, IJsonFormatter
	{
		public NewsListItemFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("gid"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("title"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("url"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("is_external_url"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("author"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("contents"),
					5
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("feedlabel"),
					6
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("date"),
					7
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("feedname"),
					8
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("feedtype"),
					9
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("gid"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("title"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("url"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("is_external_url"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("author"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("contents"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("feedlabel"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("date"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("feedname"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("feedtype")
			};
		}

		public void Serialize(ref JsonWriter writer, NewsListItem value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.gid);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.title);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.url);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteBoolean(value.is_external_url);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.author);
			writer.WriteRaw(this.____stringByteKeys[5]);
			writer.WriteString(value.contents);
			writer.WriteRaw(this.____stringByteKeys[6]);
			writer.WriteString(value.feedlabel);
			writer.WriteRaw(this.____stringByteKeys[7]);
			writer.WriteInt64(value.date);
			writer.WriteRaw(this.____stringByteKeys[8]);
			writer.WriteString(value.feedname);
			writer.WriteRaw(this.____stringByteKeys[9]);
			writer.WriteInt32(value.feedtype);
			writer.WriteEndObject();
		}

		public NewsListItem Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			string text3 = null;
			bool flag = false;
			string text4 = null;
			string text5 = null;
			string text6 = null;
			long num = 0L;
			string text7 = null;
			int num2 = 0;
			int num3 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num3))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num4;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num4))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num4)
					{
					case 0:
						text = reader.ReadString();
						break;
					case 1:
						text2 = reader.ReadString();
						break;
					case 2:
						text3 = reader.ReadString();
						break;
					case 3:
						flag = reader.ReadBoolean();
						break;
					case 4:
						text4 = reader.ReadString();
						break;
					case 5:
						text5 = reader.ReadString();
						break;
					case 6:
						text6 = reader.ReadString();
						break;
					case 7:
						num = reader.ReadInt64();
						break;
					case 8:
						text7 = reader.ReadString();
						break;
					case 9:
						num2 = reader.ReadInt32();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new NewsListItem(text, text2, text3, flag, text4, text5, text6, num, text7, num2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
