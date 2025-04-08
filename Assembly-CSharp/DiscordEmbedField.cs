using System;
using Utf8Json;

public readonly struct DiscordEmbedField : IEquatable<DiscordEmbedField>, IJsonSerializable
{
	[SerializationConstructor]
	public DiscordEmbedField(string name, string value, bool inline)
	{
		this.name = name;
		this.value = value;
		this.inline = inline;
	}

	public bool Equals(DiscordEmbedField other)
	{
		return this.name == other.name && this.value == other.value && this.inline == other.inline;
	}

	public override bool Equals(object obj)
	{
		if (obj is DiscordEmbedField)
		{
			DiscordEmbedField discordEmbedField = (DiscordEmbedField)obj;
			return this.Equals(discordEmbedField);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((this.name != null) ? this.name.GetHashCode() : 0) * 397) ^ ((this.value != null) ? this.value.GetHashCode() : 0)) * 397) ^ this.inline.GetHashCode();
	}

	public static bool operator ==(DiscordEmbedField left, DiscordEmbedField right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DiscordEmbedField left, DiscordEmbedField right)
	{
		return !left.Equals(right);
	}

	public readonly string name;

	public readonly string value;

	public readonly bool inline;
}
