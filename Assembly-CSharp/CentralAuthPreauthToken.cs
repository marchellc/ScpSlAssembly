using System;

public readonly struct CentralAuthPreauthToken
{
	public CentralAuthPreauthToken(string userId, byte flags, string country, long expiration, string signature)
	{
		this.UserId = userId;
		this.Flags = flags;
		this.Country = country;
		this.Expiration = expiration;
		this.Signature = signature;
	}

	public readonly string UserId;

	public readonly byte Flags;

	public readonly string Country;

	public readonly long Expiration;

	public readonly string Signature;
}
