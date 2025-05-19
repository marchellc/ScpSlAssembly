using System;

namespace LiteNetLib;

internal readonly struct NativeAddr : IEquatable<NativeAddr>
{
	private readonly long _part1;

	private readonly long _part2;

	private readonly long _part3;

	private readonly int _part4;

	private readonly int _hash;

	public NativeAddr(byte[] address, int len)
	{
		_part1 = BitConverter.ToInt64(address, 0);
		_part2 = BitConverter.ToInt64(address, 8);
		if (len > 16)
		{
			_part3 = BitConverter.ToInt64(address, 16);
			_part4 = BitConverter.ToInt32(address, 24);
		}
		else
		{
			_part3 = 0L;
			_part4 = 0;
		}
		_hash = (int)(_part1 >> 32) ^ (int)_part1 ^ (int)(_part2 >> 32) ^ (int)_part2 ^ (int)(_part3 >> 32) ^ (int)_part3 ^ _part4;
	}

	public override int GetHashCode()
	{
		return _hash;
	}

	public bool Equals(NativeAddr other)
	{
		if (_part1 == other._part1 && _part2 == other._part2 && _part3 == other._part3)
		{
			return _part4 == other._part4;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is NativeAddr other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(NativeAddr left, NativeAddr right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NativeAddr left, NativeAddr right)
	{
		return !left.Equals(right);
	}
}
