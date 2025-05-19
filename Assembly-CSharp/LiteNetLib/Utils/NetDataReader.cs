using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace LiteNetLib.Utils;

public class NetDataReader
{
	protected byte[] _data;

	protected int _position;

	protected int _dataSize;

	private int _offset;

	public byte[] RawData
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _data;
		}
	}

	public int RawDataSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _dataSize;
		}
	}

	public int UserDataOffset
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _offset;
		}
	}

	public int UserDataSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _dataSize - _offset;
		}
	}

	public bool IsNull
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _data == null;
		}
	}

	public int Position
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _position;
		}
	}

	public bool EndOfData
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _position == _dataSize;
		}
	}

	public int AvailableBytes
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _dataSize - _position;
		}
	}

	public void SkipBytes(int count)
	{
		_position += count;
	}

	public void SetPosition(int position)
	{
		_position = position;
	}

	public void SetSource(NetDataWriter dataWriter)
	{
		_data = dataWriter.Data;
		_position = 0;
		_offset = 0;
		_dataSize = dataWriter.Length;
	}

	public void SetSource(byte[] source)
	{
		_data = source;
		_position = 0;
		_offset = 0;
		_dataSize = source.Length;
	}

	public void SetSource(byte[] source, int offset, int maxSize)
	{
		_data = source;
		_position = offset;
		_offset = offset;
		_dataSize = maxSize;
	}

	public NetDataReader()
	{
	}

	public NetDataReader(NetDataWriter writer)
	{
		SetSource(writer);
	}

	public NetDataReader(byte[] source)
	{
		SetSource(source);
	}

	public NetDataReader(byte[] source, int offset, int maxSize)
	{
		SetSource(source, offset, maxSize);
	}

	public IPEndPoint GetNetEndPoint()
	{
		string @string = GetString(1000);
		int @int = GetInt();
		return NetUtils.MakeEndPoint(@string, @int);
	}

	public byte GetByte()
	{
		byte result = _data[_position];
		_position++;
		return result;
	}

	public sbyte GetSByte()
	{
		return (sbyte)GetByte();
	}

	public T[] GetArray<T>(ushort size)
	{
		ushort num = BitConverter.ToUInt16(_data, _position);
		_position += 2;
		T[] array = new T[num];
		num *= size;
		Buffer.BlockCopy(_data, _position, array, 0, num);
		_position += num;
		return array;
	}

	public bool[] GetBoolArray()
	{
		return GetArray<bool>(1);
	}

	public ushort[] GetUShortArray()
	{
		return GetArray<ushort>(2);
	}

	public short[] GetShortArray()
	{
		return GetArray<short>(2);
	}

	public int[] GetIntArray()
	{
		return GetArray<int>(4);
	}

	public uint[] GetUIntArray()
	{
		return GetArray<uint>(4);
	}

	public float[] GetFloatArray()
	{
		return GetArray<float>(4);
	}

	public double[] GetDoubleArray()
	{
		return GetArray<double>(8);
	}

	public long[] GetLongArray()
	{
		return GetArray<long>(8);
	}

	public ulong[] GetULongArray()
	{
		return GetArray<ulong>(8);
	}

	public string[] GetStringArray()
	{
		ushort uShort = GetUShort();
		string[] array = new string[uShort];
		for (int i = 0; i < uShort; i++)
		{
			array[i] = GetString();
		}
		return array;
	}

	public string[] GetStringArray(int maxStringLength)
	{
		ushort uShort = GetUShort();
		string[] array = new string[uShort];
		for (int i = 0; i < uShort; i++)
		{
			array[i] = GetString(maxStringLength);
		}
		return array;
	}

	public bool GetBool()
	{
		return GetByte() == 1;
	}

	public char GetChar()
	{
		return (char)GetUShort();
	}

	public ushort GetUShort()
	{
		ushort result = BitConverter.ToUInt16(_data, _position);
		_position += 2;
		return result;
	}

	public short GetShort()
	{
		short result = BitConverter.ToInt16(_data, _position);
		_position += 2;
		return result;
	}

	public long GetLong()
	{
		long result = BitConverter.ToInt64(_data, _position);
		_position += 8;
		return result;
	}

	public ulong GetULong()
	{
		ulong result = BitConverter.ToUInt64(_data, _position);
		_position += 8;
		return result;
	}

	public int GetInt()
	{
		int result = BitConverter.ToInt32(_data, _position);
		_position += 4;
		return result;
	}

	public uint GetUInt()
	{
		uint result = BitConverter.ToUInt32(_data, _position);
		_position += 4;
		return result;
	}

	public float GetFloat()
	{
		float result = BitConverter.ToSingle(_data, _position);
		_position += 4;
		return result;
	}

	public double GetDouble()
	{
		double result = BitConverter.ToDouble(_data, _position);
		_position += 8;
		return result;
	}

	public string GetString(int maxLength)
	{
		ushort uShort = GetUShort();
		if (uShort == 0)
		{
			return string.Empty;
		}
		int num = uShort - 1;
		if (num >= 65535)
		{
			return null;
		}
		ArraySegment<byte> bytesSegment = GetBytesSegment(num);
		if (maxLength <= 0 || NetDataWriter.uTF8Encoding.Value.GetCharCount(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count) <= maxLength)
		{
			return NetDataWriter.uTF8Encoding.Value.GetString(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count);
		}
		return string.Empty;
	}

	public string GetString()
	{
		ushort uShort = GetUShort();
		if (uShort == 0)
		{
			return string.Empty;
		}
		int num = uShort - 1;
		if (num >= 65535)
		{
			return null;
		}
		ArraySegment<byte> bytesSegment = GetBytesSegment(num);
		return NetDataWriter.uTF8Encoding.Value.GetString(bytesSegment.Array, bytesSegment.Offset, bytesSegment.Count);
	}

	public ArraySegment<byte> GetBytesSegment(int count)
	{
		ArraySegment<byte> result = new ArraySegment<byte>(_data, _position, count);
		_position += count;
		return result;
	}

	public ArraySegment<byte> GetRemainingBytesSegment()
	{
		ArraySegment<byte> result = new ArraySegment<byte>(_data, _position, AvailableBytes);
		_position = _data.Length;
		return result;
	}

	public T Get<T>() where T : struct, INetSerializable
	{
		T result = default(T);
		result.Deserialize(this);
		return result;
	}

	public T Get<T>(Func<T> constructor) where T : class, INetSerializable
	{
		T val = constructor();
		val.Deserialize(this);
		return val;
	}

	public byte[] GetRemainingBytes()
	{
		byte[] array = new byte[AvailableBytes];
		Buffer.BlockCopy(_data, _position, array, 0, AvailableBytes);
		_position = _data.Length;
		return array;
	}

	public void GetBytes(byte[] destination, int start, int count)
	{
		Buffer.BlockCopy(_data, _position, destination, start, count);
		_position += count;
	}

	public void GetBytes(byte[] destination, int count)
	{
		Buffer.BlockCopy(_data, _position, destination, 0, count);
		_position += count;
	}

	public sbyte[] GetSBytesWithLength()
	{
		return GetArray<sbyte>(1);
	}

	public byte[] GetBytesWithLength()
	{
		return GetArray<byte>(1);
	}

	public byte PeekByte()
	{
		return _data[_position];
	}

	public sbyte PeekSByte()
	{
		return (sbyte)_data[_position];
	}

	public bool PeekBool()
	{
		return _data[_position] == 1;
	}

	public char PeekChar()
	{
		return (char)PeekUShort();
	}

	public ushort PeekUShort()
	{
		return BitConverter.ToUInt16(_data, _position);
	}

	public short PeekShort()
	{
		return BitConverter.ToInt16(_data, _position);
	}

	public long PeekLong()
	{
		return BitConverter.ToInt64(_data, _position);
	}

	public ulong PeekULong()
	{
		return BitConverter.ToUInt64(_data, _position);
	}

	public int PeekInt()
	{
		return BitConverter.ToInt32(_data, _position);
	}

	public uint PeekUInt()
	{
		return BitConverter.ToUInt32(_data, _position);
	}

	public float PeekFloat()
	{
		return BitConverter.ToSingle(_data, _position);
	}

	public double PeekDouble()
	{
		return BitConverter.ToDouble(_data, _position);
	}

	public string PeekString(int maxLength)
	{
		ushort num = PeekUShort();
		if (num == 0)
		{
			return string.Empty;
		}
		int num2 = num - 1;
		if (num2 >= 65535)
		{
			return null;
		}
		if (maxLength <= 0 || NetDataWriter.uTF8Encoding.Value.GetCharCount(_data, _position + 2, num2) <= maxLength)
		{
			return NetDataWriter.uTF8Encoding.Value.GetString(_data, _position + 2, num2);
		}
		return string.Empty;
	}

	public string PeekString()
	{
		ushort num = PeekUShort();
		if (num == 0)
		{
			return string.Empty;
		}
		int num2 = num - 1;
		if (num2 >= 65535)
		{
			return null;
		}
		return NetDataWriter.uTF8Encoding.Value.GetString(_data, _position + 2, num2);
	}

	public bool TryGetByte(out byte result)
	{
		if (AvailableBytes >= 1)
		{
			result = GetByte();
			return true;
		}
		result = 0;
		return false;
	}

	public bool TryGetSByte(out sbyte result)
	{
		if (AvailableBytes >= 1)
		{
			result = GetSByte();
			return true;
		}
		result = 0;
		return false;
	}

	public bool TryGetBool(out bool result)
	{
		if (AvailableBytes >= 1)
		{
			result = GetBool();
			return true;
		}
		result = false;
		return false;
	}

	public bool TryGetChar(out char result)
	{
		if (!TryGetUShort(out var result2))
		{
			result = '\0';
			return false;
		}
		result = (char)result2;
		return true;
	}

	public bool TryGetShort(out short result)
	{
		if (AvailableBytes >= 2)
		{
			result = GetShort();
			return true;
		}
		result = 0;
		return false;
	}

	public bool TryGetUShort(out ushort result)
	{
		if (AvailableBytes >= 2)
		{
			result = GetUShort();
			return true;
		}
		result = 0;
		return false;
	}

	public bool TryGetInt(out int result)
	{
		if (AvailableBytes >= 4)
		{
			result = GetInt();
			return true;
		}
		result = 0;
		return false;
	}

	public bool TryGetUInt(out uint result)
	{
		if (AvailableBytes >= 4)
		{
			result = GetUInt();
			return true;
		}
		result = 0u;
		return false;
	}

	public bool TryGetLong(out long result)
	{
		if (AvailableBytes >= 8)
		{
			result = GetLong();
			return true;
		}
		result = 0L;
		return false;
	}

	public bool TryGetULong(out ulong result)
	{
		if (AvailableBytes >= 8)
		{
			result = GetULong();
			return true;
		}
		result = 0uL;
		return false;
	}

	public bool TryGetFloat(out float result)
	{
		if (AvailableBytes >= 4)
		{
			result = GetFloat();
			return true;
		}
		result = 0f;
		return false;
	}

	public bool TryGetDouble(out double result)
	{
		if (AvailableBytes >= 8)
		{
			result = GetDouble();
			return true;
		}
		result = 0.0;
		return false;
	}

	public bool TryGetString(out string result)
	{
		if (AvailableBytes >= 2)
		{
			ushort num = PeekUShort();
			if (AvailableBytes >= num + 1)
			{
				result = GetString();
				return true;
			}
		}
		result = null;
		return false;
	}

	public bool TryGetStringArray(out string[] result)
	{
		if (!TryGetUShort(out var result2))
		{
			result = null;
			return false;
		}
		result = new string[result2];
		for (int i = 0; i < result2; i++)
		{
			if (!TryGetString(out result[i]))
			{
				result = null;
				return false;
			}
		}
		return true;
	}

	public bool TryGetBytesWithLength(out byte[] result)
	{
		if (AvailableBytes >= 2)
		{
			ushort num = PeekUShort();
			if (num >= 0 && AvailableBytes >= 2 + num)
			{
				result = GetBytesWithLength();
				return true;
			}
		}
		result = null;
		return false;
	}

	public void Clear()
	{
		_position = 0;
		_dataSize = 0;
		_data = null;
	}
}
