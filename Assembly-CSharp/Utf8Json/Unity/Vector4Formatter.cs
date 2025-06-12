using System;
using UnityEngine;
using Utf8Json.Internal;

namespace Utf8Json.Unity;

public sealed class Vector4Formatter : IJsonFormatter<Vector4>, IJsonFormatter
{
	private readonly AutomataDictionary ____keyMapping;

	private readonly byte[][] ____stringByteKeys;

	public Vector4Formatter()
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
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("w"),
				3
			}
		};
		this.____stringByteKeys = new byte[4][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("x"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("y"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("z"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("w")
		};
	}

	public void Serialize(ref JsonWriter writer, Vector4 value, IJsonFormatterResolver formatterResolver)
	{
		writer.WriteRaw(this.____stringByteKeys[0]);
		writer.WriteSingle(value.x);
		writer.WriteRaw(this.____stringByteKeys[1]);
		writer.WriteSingle(value.y);
		writer.WriteRaw(this.____stringByteKeys[2]);
		writer.WriteSingle(value.z);
		writer.WriteRaw(this.____stringByteKeys[3]);
		writer.WriteSingle(value.w);
		writer.WriteEndObject();
	}

	public Vector4 Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (reader.ReadIsNull())
		{
			throw new InvalidOperationException("typecode is null, struct not supported");
		}
		float x = 0f;
		float y = 0f;
		float z = 0f;
		float w = 0f;
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
				x = reader.ReadSingle();
				break;
			case 1:
				y = reader.ReadSingle();
				break;
			case 2:
				z = reader.ReadSingle();
				break;
			case 3:
				w = reader.ReadSingle();
				break;
			default:
				reader.ReadNextBlock();
				break;
			}
		}
		Vector4 result = new Vector4(x, y, z, w);
		result.x = x;
		result.y = y;
		result.z = z;
		result.w = w;
		return result;
	}
}
