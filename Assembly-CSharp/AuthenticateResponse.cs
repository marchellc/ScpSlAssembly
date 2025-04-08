using System;
using Utf8Json;

public readonly struct AuthenticateResponse : IEquatable<AuthenticateResponse>, IJsonSerializable
{
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
		return this.success == other.success && this.error == other.error && this.token == other.token && this.id == other.id && this.nonce == other.nonce && this.country == other.country && this.flags == other.flags && this.expiration == other.expiration && this.preauth == other.preauth && this.globalBan == other.globalBan && this.lifetime == other.lifetime && this.NoWatermarking == other.NoWatermarking;
	}

	public override bool Equals(object obj)
	{
		if (obj is AuthenticateResponse)
		{
			AuthenticateResponse authenticateResponse = (AuthenticateResponse)obj;
			return this.Equals(authenticateResponse);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((((((((((((((this.success.GetHashCode() * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0)) * 397) ^ ((this.token != null) ? this.token.GetHashCode() : 0)) * 397) ^ ((this.id != null) ? this.id.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.country != null) ? this.country.GetHashCode() : 0)) * 397) ^ this.flags.GetHashCode()) * 397) ^ this.expiration.GetHashCode()) * 397) ^ ((this.preauth != null) ? this.preauth.GetHashCode() : 0)) * 397) ^ ((this.globalBan != null) ? this.globalBan.GetHashCode() : 0)) * 397) ^ this.lifetime.GetHashCode()) * 397) ^ this.NoWatermarking.GetHashCode();
	}

	public static bool operator ==(AuthenticateResponse left, AuthenticateResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(AuthenticateResponse left, AuthenticateResponse right)
	{
		return !left.Equals(right);
	}

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
}
