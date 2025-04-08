using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity
{
	public sealed class BoundsFormatter : IJsonFormatter<Bounds>, IJsonFormatter
	{
		public BoundsFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("center"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("size"),
					1
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("center"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("size")
			};
		}

		public void Serialize(ref JsonWriter writer, Bounds value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<Vector3>().Serialize(ref writer, value.center, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[1]);
			formatterResolver.GetFormatterWithVerify<Vector3>().Serialize(ref writer, value.size, formatterResolver);
			writer.WriteEndObject();
		}

		public Bounds Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			Vector3 vector = default(Vector3);
			Vector3 vector2 = default(Vector3);
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
				else if (num2 != 0)
				{
					if (num2 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						vector2 = formatterResolver.GetFormatterWithVerify<Vector3>().Deserialize(ref reader, formatterResolver);
					}
				}
				else
				{
					vector = formatterResolver.GetFormatterWithVerify<Vector3>().Deserialize(ref reader, formatterResolver);
				}
			}
			return new Bounds(vector, vector2)
			{
				center = vector,
				size = vector2
			};
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
