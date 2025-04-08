using System;

public abstract class Token
{
	protected Token(string userId, string nickname, DateTimeOffset issuanceTime, DateTimeOffset expirationTime, string usage, string issuedBy, string serial, bool testSignature, int tokenVersion)
	{
		this.UserId = userId;
		this.Nickname = nickname;
		this.IssuanceTime = issuanceTime;
		this.ExpirationTime = expirationTime;
		this.Usage = usage;
		this.IssuedBy = issuedBy;
		this.Serial = serial;
		this.TestSignature = testSignature;
		this.TokenVersion = tokenVersion;
	}

	public readonly string UserId;

	public readonly string Nickname;

	public readonly DateTimeOffset IssuanceTime;

	public readonly DateTimeOffset ExpirationTime;

	public readonly string Usage;

	public readonly string IssuedBy;

	public readonly string Serial;

	public readonly bool TestSignature;

	public readonly int TokenVersion;
}
