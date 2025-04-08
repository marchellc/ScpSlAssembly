using System;
using Utf8Json;

public readonly struct DiscordWebhook : IEquatable<DiscordWebhook>, IJsonSerializable
{
	[SerializationConstructor]
	public DiscordWebhook(string content, string username, string avatar_url, bool tts, DiscordEmbed[] embeds)
	{
		this.content = content;
		this.username = username;
		this.avatar_url = avatar_url;
		this.tts = tts;
		this.embeds = embeds;
	}

	public bool Equals(DiscordWebhook other)
	{
		return this.content == other.content && this.username == other.username && this.avatar_url == other.avatar_url && this.tts == other.tts && object.Equals(this.embeds, other.embeds);
	}

	public override bool Equals(object obj)
	{
		if (obj is DiscordWebhook)
		{
			DiscordWebhook discordWebhook = (DiscordWebhook)obj;
			return this.Equals(discordWebhook);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((this.content != null) ? this.content.GetHashCode() : 0) * 397) ^ ((this.username != null) ? this.username.GetHashCode() : 0)) * 397) ^ ((this.avatar_url != null) ? this.avatar_url.GetHashCode() : 0)) * 397) ^ this.tts.GetHashCode()) * 397) ^ ((this.embeds != null) ? this.embeds.GetHashCode() : 0);
	}

	public static bool operator ==(DiscordWebhook left, DiscordWebhook right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DiscordWebhook left, DiscordWebhook right)
	{
		return !left.Equals(right);
	}

	public readonly string content;

	public readonly string username;

	public readonly string avatar_url;

	public readonly bool tts;

	public readonly DiscordEmbed[] embeds;
}
