using System;
using Utf8Json;

public readonly struct CreditsListMember : IEquatable<CreditsListMember>, IJsonSerializable
{
	[SerializationConstructor]
	public CreditsListMember(string name, string title, string color)
	{
		this.name = name;
		this.title = title;
		this.color = color;
	}

	public bool Equals(CreditsListMember other)
	{
		return this.name == other.name && this.title == other.title && this.color == other.color;
	}

	public override bool Equals(object obj)
	{
		if (obj is CreditsListMember)
		{
			CreditsListMember creditsListMember = (CreditsListMember)obj;
			return this.Equals(creditsListMember);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (this.name == null)
		{
			return 0;
		}
		return this.name.GetHashCode();
	}

	public static bool operator ==(CreditsListMember left, CreditsListMember right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CreditsListMember left, CreditsListMember right)
	{
		return !left.Equals(right);
	}

	public readonly string name;

	public readonly string title;

	public readonly string color;
}
