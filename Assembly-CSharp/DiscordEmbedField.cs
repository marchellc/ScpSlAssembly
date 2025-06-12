using System;
using Utf8Json;

public readonly struct DiscordEmbedField : IEquatable<DiscordEmbedField>, IJsonSerializable
{
	public readonly string name;

	public readonly string value;

	public readonly bool inline;

	[SerializationConstructor]
	public DiscordEmbedField(string name, string value, bool inline)
	{
		this.name = name;
		this.value = value;
		this.inline = inline;
	}

	public bool Equals(DiscordEmbedField other)
	{
		if (this.name == other.name && this.value == other.value)
		{
			return this.inline == other.inline;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is DiscordEmbedField other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = ((((this.name != null) ? this.name.GetHashCode() : 0) * 397) ^ ((this.value != null) ? this.value.GetHashCode() : 0)) * 397;
		bool flag = this.inline;
		return num ^ flag.GetHashCode();
	}

	public static bool operator ==(DiscordEmbedField left, DiscordEmbedField right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DiscordEmbedField left, DiscordEmbedField right)
	{
		return !left.Equals(right);
	}
}
