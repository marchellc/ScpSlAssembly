using System;
using Utf8Json;

public readonly struct NewsRaw : IEquatable<NewsRaw>, IJsonSerializable
{
	[SerializationConstructor]
	public NewsRaw(NewsList appnews)
	{
		this.appnews = appnews;
	}

	public bool Equals(NewsRaw other)
	{
		return this.appnews == other.appnews;
	}

	public override bool Equals(object obj)
	{
		if (obj is NewsRaw)
		{
			NewsRaw newsRaw = (NewsRaw)obj;
			return this.Equals(newsRaw);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return this.appnews.GetHashCode();
	}

	public static bool operator ==(NewsRaw left, NewsRaw right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NewsRaw left, NewsRaw right)
	{
		return !left.Equals(right);
	}

	public readonly NewsList appnews;
}
