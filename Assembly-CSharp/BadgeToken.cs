using System;
using Utf8Json;

public class BadgeToken : Token, IJsonSerializable
{
	public readonly string BadgeText;

	public readonly string BadgeColor;

	public readonly int BadgeType;

	public readonly bool Staff;

	public readonly bool RemoteAdmin;

	public readonly bool Management;

	public readonly bool OverwatchMode;

	public readonly bool GlobalBanning;

	public readonly ulong RaPermissions;

	[SerializationConstructor]
	public BadgeToken(string badgeText, string badgeColor, int badgeType, bool staff, bool remoteAdmin, bool management, bool overwatchMode, bool globalBanning, ulong raPermissions, string userId, string nickname, DateTimeOffset issuanceTime, DateTimeOffset expirationTime, string usage, string issuedBy, string serial, bool testSignature, int tokenVersion)
		: base(userId, nickname, issuanceTime, expirationTime, usage, issuedBy, serial, testSignature, tokenVersion)
	{
		this.BadgeText = badgeText;
		this.BadgeColor = badgeColor;
		this.BadgeType = badgeType;
		this.Staff = staff;
		this.RemoteAdmin = remoteAdmin;
		this.Management = management;
		this.OverwatchMode = overwatchMode;
		this.GlobalBanning = globalBanning;
		this.RaPermissions = raPermissions;
	}
}
