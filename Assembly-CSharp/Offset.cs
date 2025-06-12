using System;
using UnityEngine;

[Serializable]
public struct Offset : IEquatable<Offset>
{
	public Vector3 position;

	public Vector3 rotation;

	public Vector3 scale;

	public bool Equals(Offset other)
	{
		if (this.position == other.position && this.rotation == other.rotation)
		{
			return this.scale == other.scale;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Offset other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((this.position.GetHashCode() * 397) ^ this.rotation.GetHashCode()) * 397) ^ this.scale.GetHashCode();
	}

	public Offset(Transform t, bool local)
	{
		this.position = (local ? t.localPosition : t.position);
		this.rotation = (local ? t.localEulerAngles : t.eulerAngles);
		this.scale = t.localScale;
	}

	public static bool operator ==(Offset left, Offset right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Offset left, Offset right)
	{
		return !left.Equals(right);
	}
}
