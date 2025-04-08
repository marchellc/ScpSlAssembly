using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity
{
	public sealed class Vector3Formatter : IJsonFormatter<Vector3>, IJsonFormatter
	{
		public Vector3Formatter()
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
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("z"),
					2
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("x"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("y"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("z")
			};
		}

		public void Serialize(ref JsonWriter writer, Vector3 value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteSingle(value.x);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteSingle(value.y);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteSingle(value.z);
			writer.WriteEndObject();
		}

		public Vector3 Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
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
						num = reader.ReadSingle();
						break;
					case 1:
						num2 = reader.ReadSingle();
						break;
					case 2:
						num3 = reader.ReadSingle();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new Vector3(num, num2, num3)
			{
				x = num,
				y = num2,
				z = num3
			};
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
