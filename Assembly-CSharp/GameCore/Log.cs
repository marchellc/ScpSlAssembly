using System;
using UnityEngine;

namespace GameCore
{
	[Serializable]
	public struct Log : IEquatable<Log>
	{
		public Log(string t, Color c, bool b)
		{
			this.text = t;
			this.color = c;
			this.nospace = b;
		}

		public bool Equals(Log other)
		{
			return this.text == other.text && this.color == other.color && this.nospace == other.nospace;
		}

		public override bool Equals(object obj)
		{
			if (obj is Log)
			{
				Log log = (Log)obj;
				return this.Equals(log);
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

		public string text;

		public Color color;

		public bool nospace;
	}
}
