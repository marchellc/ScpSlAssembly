using System;
using Utf8Json;

public readonly struct PublicKeyResponse : IEquatable<PublicKeyResponse>, IJsonSerializable
{
	public readonly string key;

	public readonly string signature;

	public readonly string credits;

	[SerializationConstructor]
	public PublicKeyResponse(string key, string signature, string credits)
	{
		this.key = key;
		this.signature = signature;
		this.credits = credits;
	}

	public bool Equals(PublicKeyResponse other)
	{
		if (this.key == other.key && this.signature == other.signature)
		{
			return this.credits == other.credits;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PublicKeyResponse other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((this.key != null) ? this.key.GetHashCode() : 0) * 397) ^ ((this.signature != null) ? this.signature.GetHashCode() : 0) ^ ((this.credits != null) ? this.credits.GetHashCode() : 0);
	}

	public static bool operator ==(PublicKeyResponse left, PublicKeyResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PublicKeyResponse left, PublicKeyResponse right)
	{
		return !left.Equals(right);
	}
}
