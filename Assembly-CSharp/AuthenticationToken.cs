using System;
using Utf8Json;

public class AuthenticationToken : Token, IJsonSerializable
{
	[SerializationConstructor]
	public AuthenticationToken(string requestIp, int asn, string globalBan, string vacSession, bool doNotTrack, bool skipIpCheck, bool bypassBans, bool bypassGeoRestrictions, bool bypassWhitelists, bool globalBadge, bool privateBetaOwnership, bool syncHashed, string publicKey, string challenge, string userId, string nickname, DateTimeOffset issuanceTime, DateTimeOffset expirationTime, string usage, string issuedBy, string serial, bool testSignature, int tokenVersion)
		: base(userId, nickname, issuanceTime, expirationTime, usage, issuedBy, serial, testSignature, tokenVersion)
	{
		this.RequestIp = requestIp;
		this.Asn = asn;
		this.GlobalBan = globalBan;
		this.VacSession = vacSession;
		this.DoNotTrack = doNotTrack;
		this.SkipIpCheck = skipIpCheck;
		this.BypassBans = bypassBans;
		this.BypassGeoRestrictions = bypassGeoRestrictions;
		this.BypassWhitelists = bypassWhitelists;
		this.GlobalBadge = globalBadge;
		this.PrivateBetaOwnership = privateBetaOwnership;
		this.SyncHashed = syncHashed;
		this.PublicKey = publicKey;
		this.Challenge = challenge;
	}

	public readonly string RequestIp;

	public readonly int Asn;

	public readonly string GlobalBan;

	public readonly string VacSession;

	public readonly bool DoNotTrack;

	public readonly bool SkipIpCheck;

	public readonly bool BypassBans;

	public readonly bool BypassGeoRestrictions;

	public readonly bool BypassWhitelists;

	public readonly bool GlobalBadge;

	public readonly bool PrivateBetaOwnership;

	public readonly bool SyncHashed;

	public readonly string PublicKey;

	public readonly string Challenge;
}
