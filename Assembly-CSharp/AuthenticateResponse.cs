using System;
using Utf8Json;

public readonly struct AuthenticateResponse : IEquatable<AuthenticateResponse>, IJsonSerializable
{
	public readonly bool success;

	public readonly string error;

	public readonly string token;

	public readonly string id;

	public readonly string nonce;

	public readonly string country;

	public readonly byte flags;

	public readonly long expiration;

	public readonly string preauth;

	public readonly string globalBan;

	public readonly ushort lifetime;

	public readonly bool NoWatermarking;

	[SerializationConstructor]
	public AuthenticateResponse(bool success, string error, string token, string id, string nonce, string country, byte flags, long expiration, string preauth, string globalBan, ushort lifetime, bool NoWatermarking)
	{
		this.success = success;
		this.error = error;
		this.token = token;
		this.id = id;
		this.nonce = nonce;
		this.country = country;
		this.flags = flags;
		this.expiration = expiration;
		this.preauth = preauth;
		this.globalBan = globalBan;
		this.lifetime = lifetime;
		this.NoWatermarking = NoWatermarking;
	}

	public bool Equals(AuthenticateResponse other)
	{
		if (this.success == other.success && this.error == other.error && this.token == other.token && this.id == other.id && this.nonce == other.nonce && this.country == other.country && this.flags == other.flags && this.expiration == other.expiration && this.preauth == other.preauth && this.globalBan == other.globalBan && this.lifetime == other.lifetime)
		{
			return this.NoWatermarking == other.NoWatermarking;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is AuthenticateResponse other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		bool flag = this.success;
		int num = ((((((((((flag.GetHashCode() * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0)) * 397) ^ ((this.token != null) ? this.token.GetHashCode() : 0)) * 397) ^ ((this.id != null) ? this.id.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.country != null) ? this.country.GetHashCode() : 0)) * 397;
		byte b = this.flags;
		int num2 = (num ^ b.GetHashCode()) * 397;
		long num3 = this.expiration;
		int num4 = (((((num2 ^ num3.GetHashCode()) * 397) ^ ((this.preauth != null) ? this.preauth.GetHashCode() : 0)) * 397) ^ ((this.globalBan != null) ? this.globalBan.GetHashCode() : 0)) * 397;
		ushort num5 = this.lifetime;
		int num6 = (num4 ^ num5.GetHashCode()) * 397;
		flag = this.NoWatermarking;
		return num6 ^ flag.GetHashCode();
	}

	public static bool operator ==(AuthenticateResponse left, AuthenticateResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(AuthenticateResponse left, AuthenticateResponse right)
	{
		return !left.Equals(right);
	}
}
