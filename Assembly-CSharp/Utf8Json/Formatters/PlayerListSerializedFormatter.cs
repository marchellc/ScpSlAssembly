using System;
using System.Collections.Generic;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class PlayerListSerializedFormatter : IJsonFormatter<PlayerListSerialized>, IJsonFormatter
	{
		public PlayerListSerializedFormatter()
		{
			this.____keyMapping = new AutomataDictionary { 
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("objects"),
				0
			} };
			this.____stringByteKeys = new byte[][] { JsonWriter.GetEncodedPropertyNameWithBeginObject("objects") };
		}

		public void Serialize(ref JsonWriter writer, PlayerListSerialized value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<List<string>>().Serialize(ref writer, value.objects, formatterResolver);
			writer.WriteEndObject();
		}

		public PlayerListSerialized Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			List<string> list = null;
			int num = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num2))
				{
					reader.ReadNextBlock();
				}
				else if (num2 == 0)
				{
					list = formatterResolver.GetFormatterWithVerify<List<string>>().Deserialize(ref reader, formatterResolver);
				}
				else
				{
					reader.ReadNextBlock();
				}
			}
			return new PlayerListSerialized(list);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
