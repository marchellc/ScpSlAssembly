using System;
using UnityEngine;

public readonly struct LowPrecisionQuaternion : IEquatable<LowPrecisionQuaternion>
{
	public LowPrecisionQuaternion(Quaternion value)
	{
		this._x = (sbyte)(value.x * 127f);
		this._y = (sbyte)(value.y * 127f);
		this._z = (sbyte)(value.z * 127f);
		this._w = (sbyte)(value.w * 127f);
	}

	public Quaternion Value
	{
		get
		{
			return new Quaternion((float)this._x / 127f, (float)this._y / 127f, (float)this._z / 127f, (float)this._w / 127f).normalized;
		}
	}

	public override int GetHashCode()
	{
		return (int)this._x | ((int)this._y << 8) | ((int)this._z << 16) | ((int)this._w << 24);
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
		return this._x == other._x && this._y == other._y && this._z == other._z && this._w == other._w;
	}

	public override bool Equals(object obj)
	{
		if (obj is LowPrecisionQuaternion)
		{
			LowPrecisionQuaternion lowPrecisionQuaternion = (LowPrecisionQuaternion)obj;
			return this.Equals(lowPrecisionQuaternion);
		}
		return false;
	}

	private const sbyte Range = 127;

	private readonly sbyte _x;

	private readonly sbyte _y;

	private readonly sbyte _z;

	private readonly sbyte _w;
}
