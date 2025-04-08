using System;
using Utf8Json;

public readonly struct NewsList : IEquatable<NewsList>, IJsonSerializable
{
	[SerializationConstructor]
	public NewsList(int appid, NewsListItem[] newsitems, int count)
	{
		this.appid = appid;
		this.newsitems = newsitems;
		this.count = count;
	}

	public bool Equals(NewsList other)
	{
		return this.appid == other.appid;
	}

	public override bool Equals(object obj)
	{
		if (obj is NewsList)
		{
			NewsList newsList = (NewsList)obj;
			return this.Equals(newsList);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return this.appid.GetHashCode();
	}

	public static bool operator ==(NewsList left, NewsList right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NewsList left, NewsList right)
	{
		return !left.Equals(right);
	}

	public readonly int appid;

	public readonly NewsListItem[] newsitems;

	public readonly int count;
}
