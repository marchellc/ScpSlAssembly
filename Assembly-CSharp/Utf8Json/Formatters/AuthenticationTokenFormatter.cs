using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters;

public sealed class AuthenticationTokenFormatter : IJsonFormatter<AuthenticationToken>, IJsonFormatter
{
	private readonly AutomataDictionary ____keyMapping;

	private readonly byte[][] ____stringByteKeys;

	public AuthenticationTokenFormatter()
	{
		this.____keyMapping = new AutomataDictionary
		{
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("RequestIp"),
				0
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Asn"),
				1
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("GlobalBan"),
				2
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("VacSession"),
				3
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("DoNotTrack"),
				4
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("SkipIpCheck"),
				5
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("BypassBans"),
				6
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("BypassGeoRestrictions"),
				7
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("BypassWhitelists"),
				8
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("GlobalBadge"),
				9
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("PrivateBetaOwnership"),
				10
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("SyncHashed"),
				11
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("PublicKey"),
				12
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Challenge"),
				13
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("UserId"),
				14
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Nickname"),
				15
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("IssuanceTime"),
				16
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("ExpirationTime"),
				17
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Usage"),
				18
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("IssuedBy"),
				19
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Serial"),
				20
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("TestSignature"),
				21
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("TokenVersion"),
				22
			}
		};
		this.____stringByteKeys = new byte[23][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("RequestIp"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Asn"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("GlobalBan"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("VacSession"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("DoNotTrack"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("SkipIpCheck"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("BypassBans"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("BypassGeoRestrictions"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("BypassWhitelists"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("GlobalBadge"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("PrivateBetaOwnership"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("SyncHashed"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("PublicKey"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Challenge"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("UserId"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Nickname"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("IssuanceTime"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("ExpirationTime"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Usage"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("IssuedBy"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Serial"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("TestSignature"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("TokenVersion")
		};
	}

	public void Serialize(ref JsonWriter writer, AuthenticationToken value, IJsonFormatterResolver formatterResolver)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		writer.WriteRaw(this.____stringByteKeys[0]);
		writer.WriteString(value.RequestIp);
		writer.WriteRaw(this.____stringByteKeys[1]);
		writer.WriteInt32(value.Asn);
		writer.WriteRaw(this.____stringByteKeys[2]);
		writer.WriteString(value.GlobalBan);
		writer.WriteRaw(this.____stringByteKeys[3]);
		writer.WriteString(value.VacSession);
		writer.WriteRaw(this.____stringByteKeys[4]);
		writer.WriteBoolean(value.DoNotTrack);
		writer.WriteRaw(this.____stringByteKeys[5]);
		writer.WriteBoolean(value.SkipIpCheck);
		writer.WriteRaw(this.____stringByteKeys[6]);
		writer.WriteBoolean(value.BypassBans);
		writer.WriteRaw(this.____stringByteKeys[7]);
		writer.WriteBoolean(value.BypassGeoRestrictions);
		writer.WriteRaw(this.____stringByteKeys[8]);
		writer.WriteBoolean(value.BypassWhitelists);
		writer.WriteRaw(this.____stringByteKeys[9]);
		writer.WriteBoolean(value.GlobalBadge);
		writer.WriteRaw(this.____stringByteKeys[10]);
		writer.WriteBoolean(value.PrivateBetaOwnership);
		writer.WriteRaw(this.____stringByteKeys[11]);
		writer.WriteBoolean(value.SyncHashed);
		writer.WriteRaw(this.____stringByteKeys[12]);
		writer.WriteString(value.PublicKey);
		writer.WriteRaw(this.____stringByteKeys[13]);
		writer.WriteString(value.Challenge);
		writer.WriteRaw(this.____stringByteKeys[14]);
		writer.WriteString(value.UserId);
		writer.WriteRaw(this.____stringByteKeys[15]);
		writer.WriteString(value.Nickname);
		writer.WriteRaw(this.____stringByteKeys[16]);
		formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Serialize(ref writer, value.IssuanceTime, formatterResolver);
		writer.WriteRaw(this.____stringByteKeys[17]);
		formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Serialize(ref writer, value.ExpirationTime, formatterResolver);
		writer.WriteRaw(this.____stringByteKeys[18]);
		writer.WriteString(value.Usage);
		writer.WriteRaw(this.____stringByteKeys[19]);
		writer.WriteString(value.IssuedBy);
		writer.WriteRaw(this.____stringByteKeys[20]);
		writer.WriteString(value.Serial);
		writer.WriteRaw(this.____stringByteKeys[21]);
		writer.WriteBoolean(value.TestSignature);
		writer.WriteRaw(this.____stringByteKeys[22]);
		writer.WriteInt32(value.TokenVersion);
		writer.WriteEndObject();
	}

	public AuthenticationToken Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (reader.ReadIsNull())
		{
			return null;
		}
		string requestIp = null;
		int asn = 0;
		string globalBan = null;
		string vacSession = null;
		bool doNotTrack = false;
		bool skipIpCheck = false;
		bool bypassBans = false;
		bool bypassGeoRestrictions = false;
		bool bypassWhitelists = false;
		bool globalBadge = false;
		bool privateBetaOwnership = false;
		bool syncHashed = false;
		string publicKey = null;
		string challenge = null;
		string userId = null;
		string nickname = null;
		DateTimeOffset issuanceTime = default(DateTimeOffset);
		DateTimeOffset expirationTime = default(DateTimeOffset);
		string usage = null;
		string issuedBy = null;
		string serial = null;
		bool testSignature = false;
		int tokenVersion = 0;
		int count = 0;
		reader.ReadIsBeginObjectWithVerify();
		while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
		{
			ArraySegment<byte> key = reader.ReadPropertyNameSegmentRaw();
			if (!this.____keyMapping.TryGetValueSafe(key, out var value))
			{
				reader.ReadNextBlock();
				continue;
			}
			switch (value)
			{
			case 0:
				requestIp = reader.ReadString();
				break;
			case 1:
				asn = reader.ReadInt32();
				break;
			case 2:
				globalBan = reader.ReadString();
				break;
			case 3:
				vacSession = reader.ReadString();
				break;
			case 4:
				doNotTrack = reader.ReadBoolean();
				break;
			case 5:
				skipIpCheck = reader.ReadBoolean();
				break;
			case 6:
				bypassBans = reader.ReadBoolean();
				break;
			case 7:
				bypassGeoRestrictions = reader.ReadBoolean();
				break;
			case 8:
				bypassWhitelists = reader.ReadBoolean();
				break;
			case 9:
				globalBadge = reader.ReadBoolean();
				break;
			case 10:
				privateBetaOwnership = reader.ReadBoolean();
				break;
			case 11:
				syncHashed = reader.ReadBoolean();
				break;
			case 12:
				publicKey = reader.ReadString();
				break;
			case 13:
				challenge = reader.ReadString();
				break;
			case 14:
				userId = reader.ReadString();
				break;
			case 15:
				nickname = reader.ReadString();
				break;
			case 16:
				issuanceTime = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
				break;
			case 17:
				expirationTime = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
				break;
			case 18:
				usage = reader.ReadString();
				break;
			case 19:
				issuedBy = reader.ReadString();
				break;
			case 20:
				serial = reader.ReadString();
				break;
			case 21:
				testSignature = reader.ReadBoolean();
				break;
			case 22:
				tokenVersion = reader.ReadInt32();
				break;
			default:
				reader.ReadNextBlock();
				break;
			}
		}
		return new AuthenticationToken(requestIp, asn, globalBan, vacSession, doNotTrack, skipIpCheck, bypassBans, bypassGeoRestrictions, bypassWhitelists, globalBadge, privateBetaOwnership, syncHashed, publicKey, challenge, userId, nickname, issuanceTime, expirationTime, usage, issuedBy, serial, testSignature, tokenVersion);
	}
}
