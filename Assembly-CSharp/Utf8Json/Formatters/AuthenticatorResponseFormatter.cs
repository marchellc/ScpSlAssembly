using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class AuthenticatorResponseFormatter : IJsonFormatter<AuthenticatorResponse>, IJsonFormatter
	{
		public AuthenticatorResponseFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("success"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("verified"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("error"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("token"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("messages"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("actions"),
					5
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("authAccepted"),
					6
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("authRejected"),
					7
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("verificationChallenge"),
					8
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("verificationResponse"),
					9
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("success"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("verified"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("error"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("token"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("messages"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("actions"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("authAccepted"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("authRejected"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("verificationChallenge"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("verificationResponse")
			};
		}

		public void Serialize(ref JsonWriter writer, AuthenticatorResponse value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteBoolean(value.success);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteBoolean(value.verified);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.error);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteString(value.token);
			writer.WriteRaw(this.____stringByteKeys[4]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.messages, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[5]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.actions, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[6]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.authAccepted, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[7]);
			formatterResolver.GetFormatterWithVerify<AuthenticatiorAuthReject[]>().Serialize(ref writer, value.authRejected, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[8]);
			writer.WriteString(value.verificationChallenge);
			writer.WriteRaw(this.____stringByteKeys[9]);
			writer.WriteString(value.verificationResponse);
			writer.WriteEndObject();
		}

		public AuthenticatorResponse Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			bool flag = false;
			bool flag2 = false;
			string text = null;
			string text2 = null;
			string[] array = null;
			string[] array2 = null;
			string[] array3 = null;
			AuthenticatiorAuthReject[] array4 = null;
			string text3 = null;
			string text4 = null;
			int num = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num2))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num2)
					{
					case 0:
						flag = reader.ReadBoolean();
						break;
					case 1:
						flag2 = reader.ReadBoolean();
						break;
					case 2:
						text = reader.ReadString();
						break;
					case 3:
						text2 = reader.ReadString();
						break;
					case 4:
						array = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 5:
						array2 = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 6:
						array3 = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 7:
						array4 = formatterResolver.GetFormatterWithVerify<AuthenticatiorAuthReject[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 8:
						text3 = reader.ReadString();
						break;
					case 9:
						text4 = reader.ReadString();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new AuthenticatorResponse(flag, flag2, text, text2, array, array2, array3, array4, text3, text4);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
