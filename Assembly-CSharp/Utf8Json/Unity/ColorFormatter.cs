using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity
{
	public sealed class ColorFormatter : IJsonFormatter<Color>, IJsonFormatter
	{
		public ColorFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("r"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("g"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("b"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("a"),
					3
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("r"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("g"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("b"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("a")
			};
		}

		public void Serialize(ref JsonWriter writer, Color value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteSingle(value.r);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteSingle(value.g);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteSingle(value.b);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteSingle(value.a);
			writer.WriteEndObject();
		}

		public Color Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
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
			return new Color(num, num2, num3, num4)
			{
				r = num,
				g = num2,
				b = num3,
				a = num4
			};
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
