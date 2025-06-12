using System;
using UnityEngine;

public readonly struct LowPrecisionQuaternion : IEquatable<LowPrecisionQuaternion>
{
	private const sbyte Range = sbyte.MaxValue;

	private readonly sbyte _x;

	private readonly sbyte _y;

	private readonly sbyte _z;

	private readonly sbyte _w;

	public Quaternion Value => new Quaternion((float)this._x / 127f, (float)this._y / 127f, (float)this._z / 127f, (float)this._w / 127f).normalized;

	public LowPrecisionQuaternion(Quaternion value)
	{
		this._x = (sbyte)(value.x * 127f);
		this._y = (sbyte)(value.y * 127f);
		this._z = (sbyte)(value.z * 127f);
		this._w = (sbyte)(value.w * 127f);
	}

	public override int GetHashCode()
	{
		return this._x | (this._y << 8) | (this._z << 16) | (this._w << 24);
	}

	public static bool operator ==(LowPrecisionQuaternion left, LowPrecisionQuaternion right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(LowPrecisionQuaternion left, LowPrecisionQuaternion right)
	{
		return !left.Equals(right);
	}

	public bool Equals(LowPrecisionQuaternion other)
	{
		if (this._x == other._x && this._y == other._y && this._z == other._z)
		{
			return this._w == other._w;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is LowPrecisionQuaternion other)
		{
			return this.Equals(other);
		}
		return false;
	}
}
