using System;
using Utf8Json;

public readonly struct CreditsListCategory : IEquatable<CreditsListCategory>, IJsonSerializable
{
	[SerializationConstructor]
	public CreditsListCategory(string category, CreditsListMember[] members)
	{
		this.category = category;
		this.members = members;
	}

	public bool Equals(CreditsListCategory other)
	{
		return this.category == other.category && this.members == other.members;
	}

	public override bool Equals(object obj)
	{
		if (obj is CreditsListCategory)
		{
			CreditsListCategory creditsListCategory = (CreditsListCategory)obj;
			return this.Equals(creditsListCategory);
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

	public readonly string category;

	public readonly CreditsListMember[] members;
}
