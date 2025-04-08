using System;
using Utf8Json;

public readonly struct RenewResponse : IEquatable<RenewResponse>, IJsonSerializable
{
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
		return this.success == other.success && this.error == other.error && this.id == other.id && this.nonce == other.nonce && this.country == other.country && this.flags == other.flags && this.expiration == other.expiration && this.preauth == other.preauth && this.globalBan == other.globalBan && this.lifetime == other.lifetime;
	}

	public override bool Equals(object obj)
	{
		if (obj is RenewResponse)
		{
			RenewResponse renewResponse = (RenewResponse)obj;
			return this.Equals(renewResponse);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((((((((((this.success.GetHashCode() * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0)) * 397) ^ ((this.id != null) ? this.id.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.country != null) ? this.country.GetHashCode() : 0)) * 397) ^ this.flags.GetHashCode()) * 397) ^ this.expiration.GetHashCode()) * 397) ^ ((this.preauth != null) ? this.preauth.GetHashCode() : 0)) * 397) ^ ((this.globalBan != null) ? this.globalBan.GetHashCode() : 0)) * 397) ^ this.lifetime.GetHashCode();
	}

	public static bool operator ==(RenewResponse left, RenewResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RenewResponse left, RenewResponse right)
	{
		return !left.Equals(right);
	}

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
}
