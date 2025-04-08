using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class AuthenticationTokenFormatter : IJsonFormatter<AuthenticationToken>, IJsonFormatter
	{
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
			this.____stringByteKeys = new byte[][]
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
			string text = null;
			int num = 0;
			string text2 = null;
			string text3 = null;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			bool flag8 = false;
			string text4 = null;
			string text5 = null;
			string text6 = null;
			string text7 = null;
			DateTimeOffset dateTimeOffset = default(DateTimeOffset);
			DateTimeOffset dateTimeOffset2 = default(DateTimeOffset);
			string text8 = null;
			string text9 = null;
			string text10 = null;
			bool flag9 = false;
			int num2 = 0;
			int num3 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num3))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num4;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num4))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num4)
					{
					case 0:
						text = reader.ReadString();
						break;
					case 1:
						num = reader.ReadInt32();
						break;
					case 2:
						text2 = reader.ReadString();
						break;
					case 3:
						text3 = reader.ReadString();
						break;
					case 4:
						flag = reader.ReadBoolean();
						break;
					case 5:
						flag2 = reader.ReadBoolean();
						break;
					case 6:
						flag3 = reader.ReadBoolean();
						break;
					case 7:
						flag4 = reader.ReadBoolean();
						break;
					case 8:
						flag5 = reader.ReadBoolean();
						break;
					case 9:
						flag6 = reader.ReadBoolean();
						break;
					case 10:
						flag7 = reader.ReadBoolean();
						break;
					case 11:
						flag8 = reader.ReadBoolean();
						break;
					case 12:
						text4 = reader.ReadString();
						break;
					case 13:
						text5 = reader.ReadString();
						break;
					case 14:
						text6 = reader.ReadString();
						break;
					case 15:
						text7 = reader.ReadString();
						break;
					case 16:
						dateTimeOffset = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
						break;
					case 17:
						dateTimeOffset2 = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
						break;
					case 18:
						text8 = reader.ReadString();
						break;
					case 19:
						text9 = reader.ReadString();
						break;
					case 20:
						text10 = reader.ReadString();
						break;
					case 21:
						flag9 = reader.ReadBoolean();
						break;
					case 22:
						num2 = reader.ReadInt32();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new AuthenticationToken(text, num, text2, text3, flag, flag2, flag3, flag4, flag5, flag6, flag7, flag8, text4, text5, text6, text7, dateTimeOffset, dateTimeOffset2, text8, text9, text10, flag9, num2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
