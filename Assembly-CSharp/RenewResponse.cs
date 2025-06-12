using System;
using Utf8Json;

public readonly struct RenewResponse : IEquatable<RenewResponse>, IJsonSerializable
{
	public readonly bool success;

	public readonly string error;

	public readonly string id;

	public readonly string nonce;

	public readonly string country;

	public readonly byte flags;

	public readonly long expiration;

	public readonly string preauth;

	public readonly string globalBan;

	public readonly ushort lifetime;

	[SerializationConstructor]
	public RenewResponse(bool success, string error, string id, string nonce, string country, byte flags, long expiration, string preauth, string globalBan, ushort lifetime)
	{
		this.success = success;
		this.error = error;
		this.id = id;
		this.country = country;
		this.nonce = nonce;
		this.flags = flags;
		this.expiration = expiration;
		this.preauth = preauth;
		this.globalBan = globalBan;
		this.lifetime = lifetime;
	}

	public bool Equals(RenewResponse other)
	{
		if (this.success == other.success && this.error == other.error && this.id == other.id && this.nonce == other.nonce && this.country == other.country && this.flags == other.flags && this.expiration == other.expiration && this.preauth == other.preauth && this.globalBan == other.globalBan)
		{
			return this.lifetime == other.lifetime;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is RenewResponse other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		bool flag = this.success;
		int num = ((((((((flag.GetHashCode() * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0)) * 397) ^ ((this.id != null) ? this.id.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.country != null) ? this.country.GetHashCode() : 0)) * 397;
		byte b = this.flags;
		int num2 = (num ^ b.GetHashCode()) * 397;
		long num3 = this.expiration;
		int num4 = (((((num2 ^ num3.GetHashCode()) * 397) ^ ((this.preauth != null) ? this.preauth.GetHashCode() : 0)) * 397) ^ ((this.globalBan != null) ? this.globalBan.GetHashCode() : 0)) * 397;
		ushort num5 = this.lifetime;
		return num4 ^ num5.GetHashCode();
	}

	public static bool operator ==(RenewResponse left, RenewResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RenewResponse left, RenewResponse right)
	{
		return !left.Equals(right);
	}
}
