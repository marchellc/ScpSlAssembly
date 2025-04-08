using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity
{
	public sealed class Vector2Formatter : IJsonFormatter<Vector2>, IJsonFormatter
	{
		public Vector2Formatter()
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
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("x"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("y")
			};
		}

		public void Serialize(ref JsonWriter writer, Vector2 value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteSingle(value.x);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteSingle(value.y);
			writer.WriteEndObject();
		}

		public Vector2 Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			float num = 0f;
			float num2 = 0f;
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
				else if (num4 != 0)
				{
					if (num4 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						num2 = reader.ReadSingle();
					}
				}
				else
				{
					num = reader.ReadSingle();
				}
			}
			return new Vector2(num, num2)
			{
				x = num,
				y = num2
			};
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
