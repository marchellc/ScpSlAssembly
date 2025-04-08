using System;
using Utf8Json;

public readonly struct DiscordEmbed : IEquatable<DiscordEmbed>, IJsonSerializable
{
	[SerializationConstructor]
	public DiscordEmbed(string title, string type, string description, int color, DiscordEmbedField[] fields)
	{
		this.title = title;
		this.type = type;
		this.description = description;
		this.color = color;
		this.fields = fields;
	}

	public bool Equals(DiscordEmbed other)
	{
		return this.title == other.title && this.type == other.type && this.description == other.description && this.color == other.color && object.Equals(this.fields, other.fields);
	}

	public override bool Equals(object obj)
	{
		if (obj is DiscordEmbed)
		{
			DiscordEmbed discordEmbed = (DiscordEmbed)obj;
			return this.Equals(discordEmbed);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((this.title != null) ? this.title.GetHashCode() : 0) * 397) ^ ((this.type != null) ? this.type.GetHashCode() : 0)) * 397) ^ ((this.description != null) ? this.description.GetHashCode() : 0)) * 397) ^ this.color) * 397) ^ ((this.fields != null) ? this.fields.GetHashCode() : 0);
	}

	public static bool operator ==(DiscordEmbed left, DiscordEmbed right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DiscordEmbed left, DiscordEmbed right)
	{
		return !left.Equals(right);
	}

	public readonly string title;

	public readonly string type;

	public readonly string description;

	public readonly int color;

	public readonly DiscordEmbedField[] fields;
}
