using System;
using System.Collections.Generic;
using Utf8Json;

public readonly struct PlayerListSerialized : IEquatable<PlayerListSerialized>, IJsonSerializable
{
	public readonly List<string> objects;

	[SerializationConstructor]
	public PlayerListSerialized(List<string> objects)
	{
		this.objects = objects;
	}

	public bool Equals(PlayerListSerialized other)
	{
		return this.objects == other.objects;
	}

	public override bool Equals(object obj)
	{
		if (obj is PlayerListSerialized other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (this.objects == null)
		{
			return 0;
		}
		return this.objects.GetHashCode();
	}

	public static bool operator ==(PlayerListSerialized left, PlayerListSerialized right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PlayerListSerialized left, PlayerListSerialized right)
	{
		return !left.Equals(right);
	}
}
