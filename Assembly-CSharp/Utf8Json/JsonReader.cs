using System;
using System.Text;
using Utf8Json.Internal;
using Utf8Json.Internal.DoubleConversion;

namespace Utf8Json
{
	public struct JsonReader
	{
		public JsonReader(byte[] bytes)
		{
			this = new JsonReader(bytes, 0);
		}

		public JsonReader(byte[] bytes, int offset)
		{
			this.bytes = bytes;
			this.offset = offset;
			if (bytes.Length >= 3 && bytes[offset] == JsonReader.bom[0] && bytes[offset + 1] == JsonReader.bom[1] && bytes[offset + 2] == JsonReader.bom[2])
			{
				offset = (this.offset = offset + 3);
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
			return new JsonParsingException(string.Concat(new string[]
			{
				"expected:'",
				expected,
				"', actual:'",
				text,
				"', at offset:",
				num.ToString()
			}), this.bytes, num, this.offset, text);
		}

		private JsonParsingException CreateParsingExceptionMessage(string message)
		{
			char c = (char)this.bytes[this.offset];
			string text = c.ToString();
			int num = this.offset;
			return new JsonParsingException(message, this.bytes, num, num, text);
		}

		private bool IsInRange
		{
			get
			{
				return this.offset < this.bytes.Length;
			}
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
				switch (this.bytes[this.offset])
				{
				case 34:
					return JsonToken.String;
				case 44:
					return JsonToken.ValueSeparator;
				case 45:
					return JsonToken.Number;
				case 48:
					return JsonToken.Number;
				case 49:
					return JsonToken.Number;
				case 50:
					return JsonToken.Number;
				case 51:
					return JsonToken.Number;
				case 52:
					return JsonToken.Number;
				case 53:
					return JsonToken.Number;
				case 54:
					return JsonToken.Number;
				case 55:
					return JsonToken.Number;
				case 56:
					return JsonToken.Number;
				case 57:
					return JsonToken.Number;
				case 58:
					return JsonToken.NameSeparator;
				case 91:
					return JsonToken.BeginArray;
				case 93:
					return JsonToken.EndArray;
				case 102:
					return JsonToken.False;
				case 110:
					return JsonToken.Null;
				case 116:
					return JsonToken.True;
				case 123:
					return JsonToken.BeginObject;
				case 125:
					return JsonToken.EndObject;
				}
				return JsonToken.None;
			}
			return JsonToken.None;
		}

		public void SkipWhiteSpace()
		{
			int i = this.offset;
			while (i < this.bytes.Length)
			{
				switch (this.bytes[i])
				{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 11:
				case 12:
				case 14:
				case 15:
				case 16:
				case 17:
				case 18:
				case 19:
				case 20:
				case 21:
				case 22:
				case 23:
				case 24:
				case 25:
				case 26:
				case 27:
				case 28:
				case 29:
				case 30:
				case 31:
				case 33:
				case 34:
				case 35:
				case 36:
				case 37:
				case 38:
				case 39:
				case 40:
				case 41:
				case 42:
				case 43:
				case 44:
				case 45:
				case 46:
					goto IL_00EC;
				case 9:
				case 10:
				case 13:
				case 32:
					break;
				case 47:
					i = JsonReader.ReadComment(this.bytes, i);
					break;
				default:
					goto IL_00EC;
				}
				i++;
				continue;
				IL_00EC:
				this.offset = i;
				return;
			}
			this.offset = this.bytes.Length;
		}

		public bool ReadIsNull()
		{
			this.SkipWhiteSpace();
			if (!this.IsInRange || this.bytes[this.offset] != 110)
			{
				return false;
			}
			if (this.bytes[this.offset + 1] == 117 && this.bytes[this.offset + 2] == 108 && this.bytes[this.offset + 3] == 108)
			{
				this.offset += 4;
				return true;
			}
			throw this.CreateParsingException("null");
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
			int num = count;
			count = num + 1;
			if (num != 0)
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
			int num = count;
			count = num + 1;
			if (num != 0)
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
			int i = this.offset;
			while (i < this.bytes.Length)
			{
				byte b = this.bytes[i];
				if (b != 34)
				{
					if (b == 92)
					{
						char c = (char)this.bytes[i + 1];
						byte b2;
						if (c <= '\\')
						{
							if (c != '"' && c != '/' && c != '\\')
							{
								goto IL_01C6;
							}
							b2 = this.bytes[i + 1];
						}
						else if (c <= 'f')
						{
							if (c != 'b')
							{
								if (c != 'f')
								{
									goto IL_01C6;
								}
								b2 = 12;
							}
							else
							{
								b2 = 8;
							}
						}
						else if (c != 'n')
						{
							switch (c)
							{
							case 'r':
								b2 = 13;
								break;
							case 's':
								goto IL_01C6;
							case 't':
								b2 = 9;
								break;
							case 'u':
							{
								if (array2 == null)
								{
									array2 = JsonReader.StringBuilderCache.GetCodePointStringBuffer();
								}
								if (num2 == 0)
								{
									if (array == null)
									{
										array = JsonReader.StringBuilderCache.GetBuffer();
									}
									int num4 = i - num3;
									BinaryUtil.EnsureCapacity(ref array, num, num4 + 1);
									Buffer.BlockCopy(this.bytes, num3, array, num, num4);
									num += num4;
								}
								if (array2.Length == num2)
								{
									Array.Resize<char>(ref array2, array2.Length * 2);
								}
								char c2 = (char)this.bytes[i + 2];
								char c3 = (char)this.bytes[i + 3];
								char c4 = (char)this.bytes[i + 4];
								char c5 = (char)this.bytes[i + 5];
								int codePoint = JsonReader.GetCodePoint(c2, c3, c4, c5);
								array2[num2++] = (char)codePoint;
								i += 5;
								this.offset += 6;
								num3 = this.offset;
								goto IL_02AC;
							}
							default:
								goto IL_01C6;
							}
						}
						else
						{
							b2 = 10;
						}
						if (array == null)
						{
							array = JsonReader.StringBuilderCache.GetBuffer();
						}
						if (num2 != 0)
						{
							BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
							num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
							num2 = 0;
						}
						int num5 = i - num3;
						BinaryUtil.EnsureCapacity(ref array, num, num5 + 1);
						Buffer.BlockCopy(this.bytes, num3, array, num, num5);
						num += num5;
						array[num++] = b2;
						i++;
						this.offset += 2;
						num3 = this.offset;
						goto IL_02AC;
						IL_01C6:
						throw this.CreateParsingExceptionMessage("Bad JSON escape.");
					}
					if (num2 != 0)
					{
						if (array == null)
						{
							array = JsonReader.StringBuilderCache.GetBuffer();
						}
						BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
						num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
						num2 = 0;
					}
					this.offset++;
					IL_02AC:
					i++;
				}
				else
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
						array = JsonReader.StringBuilderCache.GetBuffer();
					}
					if (num2 != 0)
					{
						BinaryUtil.EnsureCapacity(ref array, num, StringEncoding.UTF8.GetMaxByteCount(num2));
						num += StringEncoding.UTF8.GetBytes(array2, 0, num2, array, num);
					}
					int num6 = this.offset - num3 - 1;
					BinaryUtil.EnsureCapacity(ref array, num, num6);
					Buffer.BlockCopy(this.bytes, num3, array, num, num6);
					num += num6;
					resultBytes = array;
					resultOffset = 0;
					resultLength = num;
					return;
				}
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
				return (int)(x - '0');
			}
			if ('a' <= x && x <= 'f')
			{
				return (int)(x - 'a' + '\n');
			}
			if ('A' <= x && x <= 'F')
			{
				return (int)(x - 'A' + '\n');
			}
			throw new JsonParsingException("Invalid Character" + x.ToString());
		}

		public ArraySegment<byte> ReadStringSegmentUnsafe()
		{
			if (this.ReadIsNull())
			{
				return JsonReader.nullTokenSegment;
			}
			byte[] array;
			int num;
			int num2;
			this.ReadStringSegmentCore(out array, out num, out num2);
			return new ArraySegment<byte>(array, num, num2);
		}

		public string ReadString()
		{
			if (this.ReadIsNull())
			{
				return null;
			}
			byte[] array;
			int num;
			int num2;
			this.ReadStringSegmentCore(out array, out num, out num2);
			return Encoding.UTF8.GetString(array, num, num2);
		}

		public string ReadPropertyName()
		{
			string text = this.ReadString();
			this.ReadIsNameSeparatorWithVerify();
			return text;
		}

		public ArraySegment<byte> ReadStringSegmentRaw()
		{
			ArraySegment<byte> arraySegment = default(ArraySegment<byte>);
			if (this.ReadIsNull())
			{
				arraySegment = JsonReader.nullTokenSegment;
			}
			else
			{
				byte[] array = this.bytes;
				int num = this.offset;
				this.offset = num + 1;
				if (array[num] != 34)
				{
					throw this.CreateParsingException("\"");
				}
				int num2 = this.offset;
				for (int i = this.offset; i < this.bytes.Length; i++)
				{
					if (this.bytes[i] == 34 && this.bytes[i - 1] != 92)
					{
						this.offset = i + 1;
						arraySegment = new ArraySegment<byte>(this.bytes, num2, this.offset - num2 - 1);
						return arraySegment;
					}
				}
				throw this.CreateParsingExceptionMessage("not found end string.");
			}
			return arraySegment;
		}

		public ArraySegment<byte> ReadPropertyNameSegmentRaw()
		{
			ArraySegment<byte> arraySegment = this.ReadStringSegmentRaw();
			this.ReadIsNameSeparatorWithVerify();
			return arraySegment;
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
			else
			{
				if (this.bytes[this.offset] != 102)
				{
					throw this.CreateParsingException("true | false");
				}
				if (this.bytes[this.offset + 1] == 97 && this.bytes[this.offset + 2] == 108 && this.bytes[this.offset + 3] == 115 && this.bytes[this.offset + 4] == 101)
				{
					this.offset += 5;
					return false;
				}
				throw this.CreateParsingException("false");
			}
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
			}
			return false;
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
			case JsonToken.None:
				break;
			case JsonToken.BeginObject:
			case JsonToken.EndObject:
			case JsonToken.BeginArray:
			case JsonToken.EndArray:
			case JsonToken.ValueSeparator:
			case JsonToken.NameSeparator:
				this.offset++;
				return;
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
			case JsonToken.True:
			case JsonToken.Null:
				this.offset += 4;
				return;
			case JsonToken.False:
				this.offset += 5;
				return;
			default:
				return;
			}
		}

		public void ReadNextBlock()
		{
			this.ReadNextBlockCore(0);
		}

		private void ReadNextBlockCore(int stack)
		{
			JsonToken jsonToken = this.GetCurrentJsonToken();
			switch (jsonToken)
			{
			case JsonToken.None:
				break;
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
					return;
				}
				break;
			case JsonToken.Number:
			case JsonToken.String:
			case JsonToken.True:
			case JsonToken.False:
			case JsonToken.Null:
			case JsonToken.ValueSeparator:
			case JsonToken.NameSeparator:
				do
				{
					this.ReadNextCore(jsonToken);
					jsonToken = this.GetCurrentJsonToken();
				}
				while (stack != 0 && jsonToken >= JsonToken.Number);
				if (stack != 0)
				{
					this.ReadNextBlockCore(stack);
				}
				break;
			default:
				return;
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
			int num2;
			long num = NumberConverter.ReadInt64(this.bytes, this.offset, out num2);
			if (num2 == 0)
			{
				throw this.CreateParsingException("Number Token");
			}
			this.offset += num2;
			return num;
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
			int num2;
			ulong num = NumberConverter.ReadUInt64(this.bytes, this.offset, out num2);
			if (num2 == 0)
			{
				throw this.CreateParsingException("Number Token");
			}
			this.offset += num2;
			return num;
		}

		public float ReadSingle()
		{
			this.SkipWhiteSpace();
			int num2;
			float num = StringToDoubleConverter.ToSingle(this.bytes, this.offset, out num2);
			if (num2 == 0)
			{
				throw this.CreateParsingException("Number Token");
			}
			this.offset += num2;
			return num;
		}

		public double ReadDouble()
		{
			this.SkipWhiteSpace();
			int num2;
			double num = StringToDoubleConverter.ToDouble(this.bytes, this.offset, out num2);
			if (num2 == 0)
			{
				throw this.CreateParsingException("Number Token");
			}
			this.offset += num2;
			return num;
		}

		public ArraySegment<byte> ReadNumberSegment()
		{
			this.SkipWhiteSpace();
			int num = this.offset;
			for (int i = this.offset; i < this.bytes.Length; i++)
			{
				if (!NumberConverter.IsNumberRepresentation(this.bytes[i]))
				{
					this.offset = i;
					IL_004B:
					return new ArraySegment<byte>(this.bytes, num, this.offset - num);
				}
			}
			this.offset = this.bytes.Length;
			goto IL_004B;
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

		private static readonly ArraySegment<byte> nullTokenSegment = new ArraySegment<byte>(new byte[] { 110, 117, 108, 108 }, 0, 4);

		private static readonly byte[] bom = Encoding.UTF8.GetPreamble();

		private readonly byte[] bytes;

		private int offset;

		internal static class StringBuilderCache
		{
			public static byte[] GetBuffer()
			{
				if (JsonReader.StringBuilderCache.buffer == null)
				{
					JsonReader.StringBuilderCache.buffer = new byte[65535];
				}
				return JsonReader.StringBuilderCache.buffer;
			}

			public static char[] GetCodePointStringBuffer()
			{
				if (JsonReader.StringBuilderCache.codePointStringBuffer == null)
				{
					JsonReader.StringBuilderCache.codePointStringBuffer = new char[65535];
				}
				return JsonReader.StringBuilderCache.codePointStringBuffer;
			}

			[ThreadStatic]
			private static byte[] buffer;

			[ThreadStatic]
			private static char[] codePointStringBuffer;
		}
	}
}
