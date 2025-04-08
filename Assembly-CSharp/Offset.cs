using System;
using UnityEngine;

[Serializable]
public struct Offset : IEquatable<Offset>
{
	public bool Equals(Offset other)
	{
		return this.position == other.position && this.rotation == other.rotation && this.scale == other.scale;
	}

	public override bool Equals(object obj)
	{
		if (obj is Offset)
		{
			Offset offset = (Offset)obj;
			return this.Equals(offset);
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

	public Vector3 position;

	public Vector3 rotation;

	public Vector3 scale;
}
