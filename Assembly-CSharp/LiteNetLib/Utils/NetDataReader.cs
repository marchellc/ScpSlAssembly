using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace LiteNetLib.Utils
{
	public class NetDataReader
	{
		public byte[] RawData
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._data;
			}
		}

		public int RawDataSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._dataSize;
			}
		}

		public int UserDataOffset
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._offset;
			}
		}

		public int UserDataSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._dataSize - this._offset;
			}
		}

		public bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._data == null;
			}
		}

		public int Position
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._position;
			}
		}

		public bool EndOfData
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._position == this._dataSize;
			}
		}

		public int AvailableBytes
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._dataSize - this._position;
			}
		}

		public void SkipBytes(int count)
		{
			this._position += count;
		}

		public void SetPosition(int position)
		{
			this._position = position;
		}

		public void SetSource(NetDataWriter dataWriter)
		{
			this._data = dataWriter.Data;
			this._position = 0;
			this._offset = 0;
			this._dataSize = dataWriter.Length;
		}

		public void SetSource(byte[] source)
		{
			this._data = source;
			this._position = 0;
			this._offset = 0;
			this._dataSize = source.Length;
		}

		public void SetSource(byte[] source, int offset, int maxSize)
		{
			this._data = source;
			this._position = offset;
			this._offset = offset;
			this._dataSize = maxSize;
		}

		public NetDataReader()
		{
		}

		public NetDataReader(NetDataWriter writer)
		{
			this.SetSource(writer);
		}

		public NetDataReader(byte[] source)
		{
			this.SetSource(source);
		}

		public NetDataReader(byte[] source, int offset, int maxSize)
		{
			this.SetSource(source, offset, maxSize);
		}

		public IPEndPoint GetNetEndPoint()
		{
			string @string = this.GetString(1000);
			int @int = this.GetInt();
			return NetUtils.MakeEndPoint(@string, @int);
		}

		public byte GetByte()
		{
			byte b = this._data[this._position];
			this._position++;
			return b;
		}

		public sbyte GetSByte()
		{
			return (sbyte)this.GetByte();
		}

		public T[] GetArray<T>(ushort size)
		{
			ushort num = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			T[] array = new T[(int)num];
			num *= size;
			Buffer.BlockCopy(this._data, this._position, array, 0, (int)num);
			this._position += (int)num;
			return array;
		}

		public bool[] GetBoolArray()
		{
			return this.GetArray<bool>(1);
		}

		public ushort[] GetUShortArray()
		{
			return this.GetArray<ushort>(2);
		}

		public short[] GetShortArray()
		{
			return this.GetArray<short>(2);
		}

		public int[] GetIntArray()
		{
			return this.GetArray<int>(4);
		}

		public uint[] GetUIntArray()
		{
			return this.GetArray<uint>(4);
		}

		public float[] GetFloatArray()
		{
			return this.GetArray<float>(4);
		}

		public double[] GetDoubleArray()
		{
			return this.GetArray<double>(8);
		}

		public long[] GetLongArray()
		{
			return this.GetArray<long>(8);
		}

		public ulong[] GetULongArray()
		{
			return this.GetArray<ulong>(8);
		}

		public string[] GetStringArray()
		{
			ushort @ushort = this.GetUShort();
			string[] array = new string[(int)@ushort];
			for (int i = 0; i < (int)@ushort; i++)
			{
				array[i] = this.GetString();
			}
			return array;
		}

		public string[] GetStringArray(int maxStringLength)
		{
			ushort @ushort = this.GetUShort();
			string[] array = new string[(int)@ushort];
			for (int i = 0; i < (int)@ushort; i++)
			{
				array[i] = this.GetString(maxStringLength);
			}
			return array;
		}

		public bool GetBool()
		{
			return this.GetByte() == 1;
		}

		public char GetChar()
		{
			return (char)this.GetUShort();
		}

		public ushort GetUShort()
		{
			ushort num = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			return num;
		}

		public short GetShort()
		{
			short num = BitConverter.ToInt16(this._data, this._position);
			this._position += 2;
			return num;
		}

		public long GetLong()
		{
			long num = BitConverter.ToInt64(this._data, this._position);
			this._position += 8;
			return num;
		}

		public ulong GetULong()
		{
			ulong num = BitConverter.ToUInt64(this._data, this._position);
			this._position += 8;
			return num;
		}

		public int GetInt()
		{
			int num = BitConverter.ToInt32(this._data, this._position);
			this._position += 4;
			return num;
		}

		public uint GetUInt()
		{
			uint num = BitConverter.ToUInt32(this._data, this._position);
			this._position += 4;
			return num;
		}

		public float GetFloat()
		{
			float num = BitConverter.ToSingle(this._data, this._position);
			this._position += 4;
			return num;
		}

		public double GetDouble()
		{
			double num = BitConverter.ToDouble(this._data, this._position);
			this._position += 8;
			return num;
		}

		public string GetString(int maxLength)
		{
			ushort @ushort = this.GetUShort();
			if (@ushort == 0)
			{
				return string.Empty;
			}
			int num = (int)(@ushort - 1);
			if (num >= 65535)
			{
				return null;
			}
			ArraySegment<byte> bytesSegment = this.GetBytesSegment(num);
			if (maxLength <= 0 || NetDataWriter.uTF8Encoding.Value.GetCharCount(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count) <= maxLength)
			{
				return NetDataWriter.uTF8Encoding.Value.GetString(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count);
			}
			return string.Empty;
		}

		public string GetString()
		{
			ushort @ushort = this.GetUShort();
			if (@ushort == 0)
			{
				return string.Empty;
			}
			int num = (int)(@ushort - 1);
			if (num >= 65535)
			{
				return null;
			}
			ArraySegment<byte> bytesSegment = this.GetBytesSegment(num);
			return NetDataWriter.uTF8Encoding.Value.GetString(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count);
		}

		public ArraySegment<byte> GetBytesSegment(int count)
		{
			ArraySegment<byte> arraySegment = new ArraySegment<byte>(this._data, this._position, count);
			this._position += count;
			return arraySegment;
		}

		public ArraySegment<byte> GetRemainingBytesSegment()
		{
			ArraySegment<byte> arraySegment = new ArraySegment<byte>(this._data, this._position, this.AvailableBytes);
			this._position = this._data.Length;
			return arraySegment;
		}

		public T Get<T>() where T : struct, INetSerializable
		{
			T t = default(T);
			t.Deserialize(this);
			return t;
		}

		public T Get<T>(Func<T> constructor) where T : class, INetSerializable
		{
			T t = constructor();
			t.Deserialize(this);
			return t;
		}

		public byte[] GetRemainingBytes()
		{
			byte[] array = new byte[this.AvailableBytes];
			Buffer.BlockCopy(this._data, this._position, array, 0, this.AvailableBytes);
			this._position = this._data.Length;
			return array;
		}

		public void GetBytes(byte[] destination, int start, int count)
		{
			Buffer.BlockCopy(this._data, this._position, destination, start, count);
			this._position += count;
		}

		public void GetBytes(byte[] destination, int count)
		{
			Buffer.BlockCopy(this._data, this._position, destination, 0, count);
			this._position += count;
		}

		public sbyte[] GetSBytesWithLength()
		{
			return this.GetArray<sbyte>(1);
		}

		public byte[] GetBytesWithLength()
		{
			return this.GetArray<byte>(1);
		}

		public byte PeekByte()
		{
			return this._data[this._position];
		}

		public sbyte PeekSByte()
		{
			return (sbyte)this._data[this._position];
		}

		public bool PeekBool()
		{
			return this._data[this._position] == 1;
		}

		public char PeekChar()
		{
			return (char)this.PeekUShort();
		}

		public ushort PeekUShort()
		{
			return BitConverter.ToUInt16(this._data, this._position);
		}

		public short PeekShort()
		{
			return BitConverter.ToInt16(this._data, this._position);
		}

		public long PeekLong()
		{
			return BitConverter.ToInt64(this._data, this._position);
		}

		public ulong PeekULong()
		{
			return BitConverter.ToUInt64(this._data, this._position);
		}

		public int PeekInt()
		{
			return BitConverter.ToInt32(this._data, this._position);
		}

		public uint PeekUInt()
		{
			return BitConverter.ToUInt32(this._data, this._position);
		}

		public float PeekFloat()
		{
			return BitConverter.ToSingle(this._data, this._position);
		}

		public double PeekDouble()
		{
			return BitConverter.ToDouble(this._data, this._position);
		}

		public string PeekString(int maxLength)
		{
			ushort num = this.PeekUShort();
			if (num == 0)
			{
				return string.Empty;
			}
			int num2 = (int)(num - 1);
			if (num2 >= 65535)
			{
				return null;
			}
			if (maxLength <= 0 || NetDataWriter.uTF8Encoding.Value.GetCharCount(this._data, this._position + 2, num2) <= maxLength)
			{
				return NetDataWriter.uTF8Encoding.Value.GetString(this._data, this._position + 2, num2);
			}
			return string.Empty;
		}

		public string PeekString()
		{
			ushort num = this.PeekUShort();
			if (num == 0)
			{
				return string.Empty;
			}
			int num2 = (int)(num - 1);
			if (num2 >= 65535)
			{
				return null;
			}
			return NetDataWriter.uTF8Encoding.Value.GetString(this._data, this._position + 2, num2);
		}

		public bool TryGetByte(out byte result)
		{
			if (this.AvailableBytes >= 1)
			{
				result = this.GetByte();
				return true;
			}
			result = 0;
			return false;
		}

		public bool TryGetSByte(out sbyte result)
		{
			if (this.AvailableBytes >= 1)
			{
				result = this.GetSByte();
				return true;
			}
			result = 0;
			return false;
		}

		public bool TryGetBool(out bool result)
		{
			if (this.AvailableBytes >= 1)
			{
				result = this.GetBool();
				return true;
			}
			result = false;
			return false;
		}

		public bool TryGetChar(out char result)
		{
			ushort num;
			if (!this.TryGetUShort(out num))
			{
				result = '\0';
				return false;
			}
			result = (char)num;
			return true;
		}

		public bool TryGetShort(out short result)
		{
			if (this.AvailableBytes >= 2)
			{
				result = this.GetShort();
				return true;
			}
			result = 0;
			return false;
		}

		public bool TryGetUShort(out ushort result)
		{
			if (this.AvailableBytes >= 2)
			{
				result = this.GetUShort();
				return true;
			}
			result = 0;
			return false;
		}

		public bool TryGetInt(out int result)
		{
			if (this.AvailableBytes >= 4)
			{
				result = this.GetInt();
				return true;
			}
			result = 0;
			return false;
		}

		public bool TryGetUInt(out uint result)
		{
			if (this.AvailableBytes >= 4)
			{
				result = this.GetUInt();
				return true;
			}
			result = 0U;
			return false;
		}

		public bool TryGetLong(out long result)
		{
			if (this.AvailableBytes >= 8)
			{
				result = this.GetLong();
				return true;
			}
			result = 0L;
			return false;
		}

		public bool TryGetULong(out ulong result)
		{
			if (this.AvailableBytes >= 8)
			{
				result = this.GetULong();
				return true;
			}
			result = 0UL;
			return false;
		}

		public bool TryGetFloat(out float result)
		{
			if (this.AvailableBytes >= 4)
			{
				result = this.GetFloat();
				return true;
			}
			result = 0f;
			return false;
		}

		public bool TryGetDouble(out double result)
		{
			if (this.AvailableBytes >= 8)
			{
				result = this.GetDouble();
				return true;
			}
			result = 0.0;
			return false;
		}

		public bool TryGetString(out string result)
		{
			if (this.AvailableBytes >= 2)
			{
				ushort num = this.PeekUShort();
				if (this.AvailableBytes >= (int)(num + 1))
				{
					result = this.GetString();
					return true;
				}
			}
			result = null;
			return false;
		}

		public bool TryGetStringArray(out string[] result)
		{
			ushort num;
			if (!this.TryGetUShort(out num))
			{
				result = null;
				return false;
			}
			result = new string[(int)num];
			for (int i = 0; i < (int)num; i++)
			{
				if (!this.TryGetString(out result[i]))
				{
					result = null;
					return false;
				}
			}
			return true;
		}

		public bool TryGetBytesWithLength(out byte[] result)
		{
			if (this.AvailableBytes >= 2)
			{
				ushort num = this.PeekUShort();
				if (num >= 0 && this.AvailableBytes >= (int)(2 + num))
				{
					result = this.GetBytesWithLength();
					return true;
				}
			}
			result = null;
			return false;
		}

		public void Clear()
		{
			this._position = 0;
			this._dataSize = 0;
			this._data = null;
		}

		protected byte[] _data;

		protected int _position;

		protected int _dataSize;

		private int _offset;
	}
}
