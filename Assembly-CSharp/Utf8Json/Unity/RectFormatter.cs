using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity
{
	public sealed class RectFormatter : IJsonFormatter<Rect>, IJsonFormatter
	{
		public RectFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("x"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("y"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("width"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("height"),
					3
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("x"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("y"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("width"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("height")
			};
		}

		public void Serialize(ref JsonWriter writer, Rect value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteSingle(value.x);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteSingle(value.y);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteSingle(value.width);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteSingle(value.height);
			writer.WriteEndObject();
		}

		public Rect Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num5))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num6;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num6))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num6)
					{
					case 0:
						num = reader.ReadSingle();
						break;
					case 1:
						num2 = reader.ReadSingle();
						break;
					case 2:
						num3 = reader.ReadSingle();
						break;
					case 3:
						num4 = reader.ReadSingle();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new Rect(num, num2, num3, num4)
			{
				x = num,
				y = num2,
				width = num3,
				height = num4
			};
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
