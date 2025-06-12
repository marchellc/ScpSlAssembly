using System;
using Utf8Json;

public readonly struct CreditsList : IEquatable<CreditsList>, IJsonSerializable
{
	public readonly CreditsListCategory[] credits;

	[SerializationConstructor]
	public CreditsList(CreditsListCategory[] credits)
	{
		this.credits = credits;
	}

	public bool Equals(CreditsList other)
	{
		return this.credits == other.credits;
	}

	public override bool Equals(object obj)
	{
		if (obj is CreditsList other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (this.credits == null)
		{
			return 0;
		}
		return this.credits.GetHashCode();
	}

	public static bool operator ==(CreditsList left, CreditsList right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CreditsList left, CreditsList right)
	{
		return !left.Equals(right);
	}
}
