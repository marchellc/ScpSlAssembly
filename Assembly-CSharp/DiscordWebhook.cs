using System;
using Utf8Json;

public readonly struct DiscordWebhook : IEquatable<DiscordWebhook>, IJsonSerializable
{
	public readonly string content;

	public readonly string username;

	public readonly string avatar_url;

	public readonly bool tts;

	public readonly DiscordEmbed[] embeds;

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
		if (this.content == other.content && this.username == other.username && this.avatar_url == other.avatar_url && this.tts == other.tts)
		{
			return object.Equals(this.embeds, other.embeds);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is DiscordWebhook other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = ((((((this.content != null) ? this.content.GetHashCode() : 0) * 397) ^ ((this.username != null) ? this.username.GetHashCode() : 0)) * 397) ^ ((this.avatar_url != null) ? this.avatar_url.GetHashCode() : 0)) * 397;
		bool flag = this.tts;
		return ((num ^ flag.GetHashCode()) * 397) ^ ((this.embeds != null) ? this.embeds.GetHashCode() : 0);
	}

	public static bool operator ==(DiscordWebhook left, DiscordWebhook right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DiscordWebhook left, DiscordWebhook right)
	{
		return !left.Equals(right);
	}
}
