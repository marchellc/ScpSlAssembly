using System;
using Utf8Json;

public readonly struct CreditsListCategory : IEquatable<CreditsListCategory>, IJsonSerializable
{
	public readonly string category;

	public readonly CreditsListMember[] members;

	[SerializationConstructor]
	public CreditsListCategory(string category, CreditsListMember[] members)
	{
		this.category = category;
		this.members = members;
	}

	public bool Equals(CreditsListCategory other)
	{
		if (this.category == other.category)
		{
			return this.members == other.members;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is CreditsListCategory other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (this.category == null)
		{
			return 0;
		}
		return this.category.GetHashCode();
	}

	public static bool operator ==(CreditsListCategory left, CreditsListCategory right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CreditsListCategory left, CreditsListCategory right)
	{
		return !left.Equals(right);
	}
}
