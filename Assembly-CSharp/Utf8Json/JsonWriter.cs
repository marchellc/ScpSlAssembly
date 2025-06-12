using System;
using System.Text;
using Utf8Json.Internal;
using Utf8Json.Internal.DoubleConversion;

namespace Utf8Json;

public struct JsonWriter
{
	private static readonly byte[] emptyBytes = new byte[0];

	private byte[] buffer;

	private int offset;

	public int CurrentOffset => this.offset;

	public void AdvanceOffset(int offset)
	{
		this.offset += offset;
	}

	public static byte[] GetEncodedPropertyName(string propertyName)
	{
		JsonWriter jsonWriter = default(JsonWriter);
		jsonWriter.WritePropertyName(propertyName);
		return jsonWriter.ToUtf8ByteArray();
	}

	public static byte[] GetEncodedPropertyNameWithPrefixValueSeparator(string propertyName)
	{
		JsonWriter jsonWriter = default(JsonWriter);
		jsonWriter.WriteValueSeparator();
		jsonWriter.WritePropertyName(propertyName);
		return jsonWriter.ToUtf8ByteArray();
	}

	public static byte[] GetEncodedPropertyNameWithBeginObject(string propertyName)
	{
		JsonWriter jsonWriter = default(JsonWriter);
		jsonWriter.WriteBeginObject();
		jsonWriter.WritePropertyName(propertyName);
		return jsonWriter.ToUtf8ByteArray();
	}

	public static byte[] GetEncodedPropertyNameWithoutQuotation(string propertyName)
	{
		JsonWriter jsonWriter = default(JsonWriter);
		jsonWriter.WriteString(propertyName);
		ArraySegment<byte> arraySegment = jsonWriter.GetBuffer();
		byte[] array = new byte[arraySegment.Count - 2];
		Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset + 1, array, 0, array.Length);
		return array;
	}

	public JsonWriter(byte[] initialBuffer)
	{
		this.buffer = initialBuffer;
		this.offset = 0;
	}

	public ArraySegment<byte> GetBuffer()
	{
		if (this.buffer == null)
		{
			return new ArraySegment<byte>(JsonWriter.emptyBytes, 0, 0);
		}
		return new ArraySegment<byte>(this.buffer, 0, this.offset);
	}

	public byte[] ToUtf8ByteArray()
	{
		if (this.buffer == null)
		{
			return JsonWriter.emptyBytes;
		}
		return BinaryUtil.FastCloneWithResize(this.buffer, this.offset);
	}

	public override string ToString()
	{
		if (this.buffer == null)
		{
			return null;
		}
		return Encoding.UTF8.GetString(this.buffer, 0, this.offset);
	}

	public void EnsureCapacity(int appendLength)
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, appendLength);
	}

	public void WriteRaw(byte rawValue)
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = rawValue;
	}

	public void WriteRaw(byte[] rawValue)
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, rawValue.Length);
		Buffer.BlockCopy(rawValue, 0, this.buffer, this.offset, rawValue.Length);
		this.offset += rawValue.Length;
	}

	public void WriteRawUnsafe(byte rawValue)
	{
		this.buffer[this.offset++] = rawValue;
	}

	public void WriteBeginArray()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 91;
	}

	public void WriteEndArray()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 93;
	}

	public void WriteBeginObject()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 123;
	}

	public void WriteEndObject()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 125;
	}

	public void WriteValueSeparator()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 44;
	}

	public void WriteNameSeparator()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 58;
	}

	public void WritePropertyName(string propertyName)
	{
		this.WriteString(propertyName);
		this.WriteNameSeparator();
	}

	public void WriteQuotation()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
		this.buffer[this.offset++] = 34;
	}

	public void WriteNull()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 4);
		this.buffer[this.offset] = 110;
		this.buffer[this.offset + 1] = 117;
		this.buffer[this.offset + 2] = 108;
		this.buffer[this.offset + 3] = 108;
		this.offset += 4;
	}

	public void WriteBoolean(bool value)
	{
		if (value)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 4);
			this.buffer[this.offset] = 116;
			this.buffer[this.offset + 1] = 114;
			this.buffer[this.offset + 2] = 117;
			this.buffer[this.offset + 3] = 101;
			this.offset += 4;
		}
		else
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 5);
			this.buffer[this.offset] = 102;
			this.buffer[this.offset + 1] = 97;
			this.buffer[this.offset + 2] = 108;
			this.buffer[this.offset + 3] = 115;
			this.buffer[this.offset + 4] = 101;
			this.offset += 5;
		}
	}

	public void WriteTrue()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 4);
		this.buffer[this.offset] = 116;
		this.buffer[this.offset + 1] = 114;
		this.buffer[this.offset + 2] = 117;
		this.buffer[this.offset + 3] = 101;
		this.offset += 4;
	}

	public void WriteFalse()
	{
		BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 5);
		this.buffer[this.offset] = 102;
		this.buffer[this.offset + 1] = 97;
		this.buffer[this.offset + 2] = 108;
		this.buffer[this.offset + 3] = 115;
		this.buffer[this.offset + 4] = 101;
		this.offset += 5;
	}

	public void WriteSingle(float value)
	{
		this.offset += DoubleToStringConverter.GetBytes(ref this.buffer, this.offset, value);
	}

	public void WriteDouble(double value)
	{
		this.offset += DoubleToStringConverter.GetBytes(ref this.buffer, this.offset, value);
	}

	public void WriteByte(byte value)
	{
		this.WriteUInt64(value);
	}

	public void WriteUInt16(ushort value)
	{
		this.WriteUInt64(value);
	}

	public void WriteUInt32(uint value)
	{
		this.WriteUInt64(value);
	}

	public void WriteUInt64(ulong value)
	{
		this.offset += NumberConverter.WriteUInt64(ref this.buffer, this.offset, value);
	}

	public void WriteSByte(sbyte value)
	{
		this.WriteInt64(value);
	}

	public void WriteInt16(short value)
	{
		this.WriteInt64(value);
	}

	public void WriteInt32(int value)
	{
		this.WriteInt64(value);
	}

	public void WriteInt64(long value)
	{
		this.offset += NumberConverter.WriteInt64(ref this.buffer, this.offset, value);
	}

	public void WriteString(string value)
	{
		if (value == null)
		{
			this.WriteNull();
			return;
		}
		int num = this.offset;
		int num2 = StringEncoding.UTF8.GetMaxByteCount(value.Length) + 2;
		BinaryUtil.EnsureCapacity(ref this.buffer, num, num2);
		int num3 = 0;
		_ = value.Length;
		this.buffer[this.offset++] = 34;
		for (int i = 0; i < value.Length; i++)
		{
			byte b = 0;
			switch (value[i])
			{
			case '"':
				b = 34;
				break;
			case '\\':
				b = 92;
				break;
			case '\b':
				b = 98;
				break;
			case '\f':
				b = 102;
				break;
			case '\n':
				b = 110;
				break;
			case '\r':
				b = 114;
				break;
			case '\t':
				b = 116;
				break;
			default:
				continue;
			}
			num2 += 2;
			BinaryUtil.EnsureCapacity(ref this.buffer, num, num2);
			this.offset += StringEncoding.UTF8.GetBytes(value, num3, i - num3, this.buffer, this.offset);
			num3 = i + 1;
			this.buffer[this.offset++] = 92;
			this.buffer[this.offset++] = b;
		}
		if (num3 != value.Length)
		{
			this.offset += StringEncoding.UTF8.GetBytes(value, num3, value.Length - num3, this.buffer, this.offset);
		}
		this.buffer[this.offset++] = 34;
	}
}
