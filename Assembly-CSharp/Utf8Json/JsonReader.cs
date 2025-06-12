using System;
using System.Text;
using Utf8Json.Internal;
using Utf8Json.Internal.DoubleConversion;

namespace Utf8Json;

public struct JsonReader
{
	internal static class StringBuilderCache
	{
		[ThreadStatic]
		private static byte[] buffer;

		[ThreadStatic]
		private static char[] codePointStringBuffer;

		public static byte[] GetBuffer()
		{
			if (StringBuilderCache.buffer == null)
			{
				StringBuilderCache.buffer = new byte[65535];
			}
			return StringBuilderCache.buffer;
		}

		public static char[] GetCodePointStringBuffer()
		{
			if (StringBuilderCache.codePointStringBuffer == null)
			{
				StringBuilderCache.codePointStringBuffer = new char[65535];
			}
			return StringBuilderCache.codePointStringBuffer;
		}
	}

	private static readonly ArraySegment<byte> nullTokenSegment = new ArraySegment<byte>(new byte[4] { 110, 117, 108, 108 }, 0, 4);

	private static readonly byte[] bom = Encoding.UTF8.GetPreamble();

	private readonly byte[] bytes;

	private int offset;

	private bool IsInRange => this.offset < this.bytes.Length;

	public JsonReader(byte[] bytes)
		: this(bytes, 0)
	{
	}

	public JsonReader(byte[] bytes, int offset)
	{
		this.bytes = bytes;
		this.offset = offset;
		if (bytes.Length >= 3 && bytes[offset] == JsonReader.bom[0] && bytes[offset + 1] == JsonReader.bom[1] && bytes[offset + 2] == JsonReader.bom[2])
		{
			this.offset = (offset += 3);
		}
	}

	private JsonParsingException CreateParsingException(string expected)
	{
		char c = (char)this.bytes[this.offset];
		string text = c.ToString();
		int num = this.offset;
		try
		{
			switch (this.GetCurrentJsonToken())
			{
			case JsonToken.Number:
			{
				ArraySegment<byte> arraySegment = this.ReadNumberSegment();
				text = StringEncoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
				break;
			}
			case JsonToken.String:
				text = "\"" + this.ReadString() + "\"";
				break;
			case JsonToken.True:
				text = "true";
				break;
			case JsonToken.False:
				text = "false";
				break;
			case JsonToken.Null:
				text = "null";
				break;
			}
		}
		catch
		{
		}
		return new JsonParsingException("expected:'" + expected + "', actual:'" + text + "', at offset:" + num, this.bytes, num, this.offset, text);
	}

	private JsonParsingException CreateParsingExceptionMessage(string message)
	{
		char c = (char)this.bytes[this.offset];
		string actualChar = c.ToString();
		int limit = this.offset;
		return new JsonParsingException(message, this.bytes, limit, limit, actualChar);
	}

	public void AdvanceOffset(int offset)
	{
		this.offset += offset;
	}

	public byte[] GetBufferUnsafe()
	{
		return this.bytes;
	}

	public int GetCurrentOffsetUnsafe()
	{
		return this.offset;
	}

	public JsonToken GetCurrentJsonToken()
	{
		this.SkipWhiteSpace();
		if (this.offset < this.bytes.Length)
		{
			return this.bytes[this.offset] switch
			{
				123 => JsonToken.BeginObject, 
				125 => JsonToken.EndObject, 
				91 => JsonToken.BeginArray, 
				93 => JsonToken.EndArray, 
				116 => JsonToken.True, 
				102 => JsonToken.False, 
				110 => JsonToken.Null, 
				44 => JsonToken.ValueSeparator, 
				58 => JsonToken.NameSeparator, 
				45 => JsonToken.Number, 
				48 => JsonToken.Number, 
				49 => JsonToken.Number, 
				50 => JsonToken.Number, 
				51 => JsonToken.Number, 
				52 => JsonToken.Number, 
				53 => JsonToken.Number, 
				54 => JsonToken.Number, 
				55 => JsonToken.Number, 
				56 => JsonToken.Number, 
				57 => JsonToken.Number, 
				34 => JsonToken.String, 
				_ => JsonToken.None, 
			};
		}
		return JsonToken.None;
	}

	public void SkipWhiteSpace()
	{
		for (int i = this.offset; i < this.bytes.Length; i++)
		{
			switch (this.bytes[i])
			{
			case 47:
				i = JsonReader.ReadComment(this.bytes, i);
				break;
			default:
				this.offset = i;
				return;
			case 9:
			case 10:
			case 13:
			case 32:
				break;
			}
		}
		this.offset = this.bytes.Length;
	}

	public bool ReadIsNull()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 110)
		{
			if (this.bytes[this.offset + 1] == 117 && this.bytes[this.offset + 2] == 108 && this.bytes[this.offset + 3] == 108)
			{
				this.offset += 4;
				return true;
			}
			throw this.CreateParsingException("null");
		}
		return false;
	}

	public bool ReadIsBeginArray()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 91)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsBeginArrayWithVerify()
	{
		if (!this.ReadIsBeginArray())
		{
			throw this.CreateParsingException("[");
		}
	}

	public bool ReadIsEndArray()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 93)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsEndArrayWithVerify()
	{
		if (!this.ReadIsEndArray())
		{
			throw this.CreateParsingException("]");
		}
	}

	public bool ReadIsEndArrayWithSkipValueSeparator(ref int count)
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 93)
		{
			this.offset++;
			return true;
		}
		if (count++ != 0)
		{
			this.ReadIsValueSeparatorWithVerify();
		}
		return false;
	}

	public bool ReadIsInArray(ref int count)
	{
		if (count == 0)
		{
			this.ReadIsBeginArrayWithVerify();
			if (this.ReadIsEndArray())
			{
				return false;
			}
		}
		else
		{
			if (this.ReadIsEndArray())
			{
				return false;
			}
			this.ReadIsValueSeparatorWithVerify();
		}
		count++;
		return true;
	}

	public bool ReadIsBeginObject()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 123)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsBeginObjectWithVerify()
	{
		if (!this.ReadIsBeginObject())
		{
			throw this.CreateParsingException("{");
		}
	}

	public bool ReadIsEndObject()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 125)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsEndObjectWithVerify()
	{
		if (!this.ReadIsEndObject())
		{
			throw this.CreateParsingException("}");
		}
	}

	public bool ReadIsEndObjectWithSkipValueSeparator(ref int count)
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 125)
		{
			this.offset++;
			return true;
		}
		if (count++ != 0)
		{
			this.ReadIsValueSeparatorWithVerify();
		}
		return false;
	}

	public bool ReadIsInObject(ref int count)
	{
		if (count == 0)
		{
			this.ReadIsBeginObjectWithVerify();
			if (this.ReadIsEndObject())
			{
				return false;
			}
		}
		else
		{
			if (this.ReadIsEndObject())
			{
				return false;
			}
			this.ReadIsValueSeparatorWithVerify();
		}
		count++;
		return true;
	}

	public bool ReadIsValueSeparator()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 44)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsValueSeparatorWithVerify()
	{
		if (!this.ReadIsValueSeparator())
		{
			throw this.CreateParsingException(",");
		}
	}

	public bool ReadIsNameSeparator()
	{
		this.SkipWhiteSpace();
		if (this.IsInRange && this.bytes[this.offset] == 58)
		{
			this.offset++;
			return true;
		}
		return false;
	}

	public void ReadIsNameSeparatorWithVerify()
	{
		if (!this.ReadIsNameSeparator())
		{
			throw this.CreateParsingException(":");
		}
	}

	private void ReadStringSegmentCore(out byte[] resultBytes, out int resultOffset, out int resultLength)
	{
		byte[] array = null;
		int num = 0;
		char[] array2 = null;
		int num2 = 0;
		if (this.bytes[this.offset] != 34)
		{
			throw this.CreateParsingException("String Begin Token");
		}
		this.offset++;
		int num3 = this.offset;
		for (int i = this.offset; i < this.bytes.Length; i++)
		{
			byte b = 0;
			switch (this.bytes[i])
			{
			case 92:
			{
				switch ((char)this.bytes[i + 1])
				{
				case '"':
				case '/':
				case '\\':
					b = this.bytes[i + 1];
					break;
				case 'b':
					b = 8;
					break;
				case 'f':
					b = 12;
					break;
				case 'n':
					b = 10;
					break;
				case 'r':
					b = 13;
					break;
				case 't':
					b = 9;
					break;
				case 'u':
				{
					if (array2 == null)
					{
						array2 = StringBuilderCache.GetCodePointStringBuffer();
					}
					if (num2 == 0)
					{
						if (array == null)
						{
							array = StringBuilderCache.GetBuffer();
						}
						int num5 = i - num3;
						BinaryUtil.EnsureCapacity(ref array, num, num5 + 1);
						Buffer.BlockCopy(this.bytes, num3, array, num, num5);
						num += num5;
					}
					if (array2.Length == num2)
					{
						Array.Resize(ref array2, array2.Length * 2);
					}
					byte a = this.bytes[i + 2];
					char b2 = (char)this.bytes[i + 3];
					char c = (char)this.bytes[i + 4];
					char d = (char)this.bytes[i + 5];
					int codePoint = JsonReader.GetCodePoint((char)a, b2, c, d);
					array2[num2++] = (char)codePoint;
					i += 5;
					this.offset += 6;
					num3 = this.offset;
					continue;
				}
				default:
					throw this.CreateParsingExceptionMessage("Bad JSON escape.");
				}
				if (array == null)
				{
					array = StringBuilderCache.GetBuffer();
				}
				if (num2 != 0)
				{
					BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
					num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
					num2 = 0;
				}
				int num6 = i - num3;
				BinaryUtil.EnsureCapacity(ref array, num, num6 + 1);
				Buffer.BlockCopy(this.bytes, num3, array, num, num6);
				num += num6;
				array[num++] = b;
				i++;
				this.offset += 2;
				num3 = this.offset;
				continue;
			}
			case 34:
			{
				this.offset++;
				if (num == 0 && num2 == 0)
				{
					resultBytes = this.bytes;
					resultOffset = num3;
					resultLength = this.offset - 1 - num3;
					return;
				}
				if (array == null)
				{
					array = StringBuilderCache.GetBuffer();
				}
				if (num2 != 0)
				{
					BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
					num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
					num2 = 0;
				}
				int num4 = this.offset - num3 - 1;
				BinaryUtil.EnsureCapacity(ref array, num, num4);
				Buffer.BlockCopy(this.bytes, num3, array, num, num4);
				num += num4;
				resultBytes = array;
				resultOffset = 0;
				resultLength = num;
				return;
			}
			}
			if (num2 != 0)
			{
				if (array == null)
				{
					array = StringBuilderCache.GetBuffer();
				}
				BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
				num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
				num2 = 0;
			}
			this.offset++;
		}
		resultLength = 0;
		resultBytes = null;
		resultOffset = 0;
		throw this.CreateParsingException("String End Token");
	}

	private static int GetCodePoint(char a, char b, char c, char d)
	{
		return ((JsonReader.ToNumber(a) * 16 + JsonReader.ToNumber(b)) * 16 + JsonReader.ToNumber(c)) * 16 + JsonReader.ToNumber(d);
	}

	private static int ToNumber(char x)
	{
		if ('0' <= x && x <= '9')
		{
			return x - 48;
		}
		if ('a' <= x && x <= 'f')
		{
			return x - 97 + 10;
		}
		if ('A' <= x && x <= 'F')
		{
			return x - 65 + 10;
		}
		throw new JsonParsingException("Invalid Character" + x);
	}

	public ArraySegment<byte> ReadStringSegmentUnsafe()
	{
		if (this.ReadIsNull())
		{
			return JsonReader.nullTokenSegment;
		}
		this.ReadStringSegmentCore(out var resultBytes, out var resultOffset, out var resultLength);
		return new ArraySegment<byte>(resultBytes, resultOffset, resultLength);
	}

	public string ReadString()
	{
		if (this.ReadIsNull())
		{
			return null;
		}
		this.ReadStringSegmentCore(out var resultBytes, out var resultOffset, out var resultLength);
		return Encoding.UTF8.GetString(resultBytes, resultOffset, resultLength);
	}

	public string ReadPropertyName()
	{
		string result = this.ReadString();
		this.ReadIsNameSeparatorWithVerify();
		return result;
	}

	public ArraySegment<byte> ReadStringSegmentRaw()
	{
		ArraySegment<byte> arraySegment = default(ArraySegment<byte>);
		if (this.ReadIsNull())
		{
			return JsonReader.nullTokenSegment;
		}
		if (this.bytes[this.offset++] != 34)
		{
			throw this.CreateParsingException("\"");
		}
		int num = this.offset;
		for (int i = this.offset; i < this.bytes.Length; i++)
		{
			if (this.bytes[i] == 34 && this.bytes[i - 1] != 92)
			{
				this.offset = i + 1;
				return new ArraySegment<byte>(this.bytes, num, this.offset - num - 1);
			}
		}
		throw this.CreateParsingExceptionMessage("not found end string.");
	}

	public ArraySegment<byte> ReadPropertyNameSegmentRaw()
	{
		ArraySegment<byte> result = this.ReadStringSegmentRaw();
		this.ReadIsNameSeparatorWithVerify();
		return result;
	}

	public bool ReadBoolean()
	{
		this.SkipWhiteSpace();
		if (this.bytes[this.offset] == 116)
		{
			if (this.bytes[this.offset + 1] == 114 && this.bytes[this.offset + 2] == 117 && this.bytes[this.offset + 3] == 101)
			{
				this.offset += 4;
				return true;
			}
			throw this.CreateParsingException("true");
		}
		if (this.bytes[this.offset] == 102)
		{
			if (this.bytes[this.offset + 1] == 97 && this.bytes[this.offset + 2] == 108 && this.bytes[this.offset + 3] == 115 && this.bytes[this.offset + 4] == 101)
			{
				this.offset += 5;
				return false;
			}
			throw this.CreateParsingException("false");
		}
		throw this.CreateParsingException("true | false");
	}

	private static bool IsWordBreak(byte c)
	{
		switch (c)
		{
		case 32:
		case 34:
		case 44:
		case 58:
		case 91:
		case 93:
		case 123:
		case 125:
			return true;
		default:
			return false;
		}
	}

	public void ReadNext()
	{
		JsonToken currentJsonToken = this.GetCurrentJsonToken();
		this.ReadNextCore(currentJsonToken);
	}

	private void ReadNextCore(JsonToken token)
	{
		switch (token)
		{
		case JsonToken.BeginObject:
		case JsonToken.EndObject:
		case JsonToken.BeginArray:
		case JsonToken.EndArray:
		case JsonToken.ValueSeparator:
		case JsonToken.NameSeparator:
			this.offset++;
			break;
		case JsonToken.True:
		case JsonToken.Null:
			this.offset += 4;
			break;
		case JsonToken.False:
			this.offset += 5;
			break;
		case JsonToken.String:
		{
			this.offset++;
			for (int j = this.offset; j < this.bytes.Length; j++)
			{
				if (this.bytes[j] == 34 && this.bytes[j - 1] != 92)
				{
					this.offset = j + 1;
					return;
				}
			}
			throw this.CreateParsingExceptionMessage("not found end string.");
		}
		case JsonToken.Number:
		{
			for (int i = this.offset; i < this.bytes.Length; i++)
			{
				if (JsonReader.IsWordBreak(this.bytes[i]))
				{
					this.offset = i;
					return;
				}
			}
			this.offset = this.bytes.Length;
			break;
		}
		case JsonToken.None:
			break;
		}
	}

	public void ReadNextBlock()
	{
		this.ReadNextBlockCore(0);
	}

	private void ReadNextBlockCore(int stack)
	{
		JsonToken currentJsonToken = this.GetCurrentJsonToken();
		switch (currentJsonToken)
		{
		default:
			return;
		case JsonToken.BeginObject:
		case JsonToken.BeginArray:
			this.offset++;
			this.ReadNextBlockCore(stack + 1);
			return;
		case JsonToken.EndObject:
		case JsonToken.EndArray:
			this.offset++;
			if (stack - 1 != 0)
			{
				this.ReadNextBlockCore(stack - 1);
			}
			return;
		case JsonToken.Number:
		case JsonToken.String:
		case JsonToken.True:
		case JsonToken.False:
		case JsonToken.Null:
		case JsonToken.ValueSeparator:
		case JsonToken.NameSeparator:
			break;
		case JsonToken.None:
			return;
		}
		do
		{
			this.ReadNextCore(currentJsonToken);
			currentJsonToken = this.GetCurrentJsonToken();
		}
		while (stack != 0 && (int)currentJsonToken >= 5);
		if (stack != 0)
		{
			this.ReadNextBlockCore(stack);
		}
	}

	public ArraySegment<byte> ReadNextBlockSegment()
	{
		int num = this.offset;
		this.ReadNextBlock();
		return new ArraySegment<byte>(this.bytes, num, this.offset - num);
	}

	public sbyte ReadSByte()
	{
		return checked((sbyte)this.ReadInt64());
	}

	public short ReadInt16()
	{
		return checked((short)this.ReadInt64());
	}

	public int ReadInt32()
	{
		return checked((int)this.ReadInt64());
	}

	public long ReadInt64()
	{
		this.SkipWhiteSpace();
		int readCount;
		long result = NumberConverter.ReadInt64(this.bytes, this.offset, out readCount);
		if (readCount == 0)
		{
			throw this.CreateParsingException("Number Token");
		}
		this.offset += readCount;
		return result;
	}

	public byte ReadByte()
	{
		return checked((byte)this.ReadUInt64());
	}

	public ushort ReadUInt16()
	{
		return checked((ushort)this.ReadUInt64());
	}

	public uint ReadUInt32()
	{
		return checked((uint)this.ReadUInt64());
	}

	public ulong ReadUInt64()
	{
		this.SkipWhiteSpace();
		int readCount;
		ulong result = NumberConverter.ReadUInt64(this.bytes, this.offset, out readCount);
		if (readCount == 0)
		{
			throw this.CreateParsingException("Number Token");
		}
		this.offset += readCount;
		return result;
	}

	public float ReadSingle()
	{
		this.SkipWhiteSpace();
		int readCount;
		float result = StringToDoubleConverter.ToSingle(this.bytes, this.offset, out readCount);
		if (readCount == 0)
		{
			throw this.CreateParsingException("Number Token");
		}
		this.offset += readCount;
		return result;
	}

	public double ReadDouble()
	{
		this.SkipWhiteSpace();
		int readCount;
		double result = StringToDoubleConverter.ToDouble(this.bytes, this.offset, out readCount);
		if (readCount == 0)
		{
			throw this.CreateParsingException("Number Token");
		}
		this.offset += readCount;
		return result;
	}

	public ArraySegment<byte> ReadNumberSegment()
	{
		this.SkipWhiteSpace();
		int num = this.offset;
		int num2 = this.offset;
		while (true)
		{
			if (num2 < this.bytes.Length)
			{
				if (!NumberConverter.IsNumberRepresentation(this.bytes[num2]))
				{
					this.offset = num2;
					break;
				}
				num2++;
				continue;
			}
			this.offset = this.bytes.Length;
			break;
		}
		return new ArraySegment<byte>(this.bytes, num, this.offset - num);
	}

	private static int ReadComment(byte[] bytes, int offset)
	{
		if (bytes[offset + 1] == 47)
		{
			offset += 2;
			for (int i = offset; i < bytes.Length; i++)
			{
				if (bytes[i] == 13 || bytes[i] == 10)
				{
					return i;
				}
			}
			throw new JsonParsingException("Can not find end token of single line comment(\r or \n).");
		}
		if (bytes[offset + 1] == 42)
		{
			offset += 2;
			for (int j = offset; j < bytes.Length; j++)
			{
				if (bytes[j] == 42 && bytes[j + 1] == 47)
				{
					return j + 1;
				}
			}
			throw new JsonParsingException("Can not find end token of multi line comment(*/).");
		}
		return offset;
	}
}
