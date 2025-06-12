using System;
using UnityEngine;

namespace GameCore;

[Serializable]
public struct Log : IEquatable<Log>
{
	public string text;

	public Color color;

	public bool nospace;

	public Log(string t, Color c, bool b)
	{
		this.text = t;
		this.color = c;
		this.nospace = b;
	}

	public bool Equals(Log other)
	{
		if (this.text == other.text && this.color == other.color)
		{
			return this.nospace == other.nospace;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Log other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((this.text != null) ? this.text.GetHashCode() : 0) * 397) ^ this.color.GetHashCode()) * 397) ^ this.nospace.GetHashCode();
	}

	public static bool operator ==(Log left, Log right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Log left, Log right)
	{
		return !left.Equals(right);
	}
}
