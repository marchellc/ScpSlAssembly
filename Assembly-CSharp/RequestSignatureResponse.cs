using System;
using Utf8Json;

public readonly struct RequestSignatureResponse : IEquatable<RequestSignatureResponse>, IJsonSerializable
{
	public readonly bool success;

	public readonly string error;

	public readonly SignedToken authToken;

	public readonly SignedToken badgeToken;

	public readonly string nonce;

	[SerializationConstructor]
	public RequestSignatureResponse(bool success, string error, SignedToken authToken, SignedToken badgeToken, string nonce)
	{
		this.success = success;
		this.error = error;
		this.authToken = authToken;
		this.badgeToken = badgeToken;
		this.nonce = nonce;
	}

	public bool Equals(RequestSignatureResponse other)
	{
		if (this.success == other.success && this.error == other.error && this.authToken == other.authToken && this.badgeToken == other.badgeToken)
		{
			return this.nonce == other.nonce;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is RequestSignatureResponse other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		bool flag = this.success;
		return (((((((flag.GetHashCode() * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0)) * 397) ^ ((this.authToken != null) ? this.authToken.GetHashCode() : 0)) * 397) ^ ((this.badgeToken != null) ? this.badgeToken.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0);
	}

	public static bool operator ==(RequestSignatureResponse left, RequestSignatureResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RequestSignatureResponse left, RequestSignatureResponse right)
	{
		return !left.Equals(right);
	}
}
