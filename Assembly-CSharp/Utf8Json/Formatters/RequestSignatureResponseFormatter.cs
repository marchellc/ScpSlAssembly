using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class RequestSignatureResponseFormatter : IJsonFormatter<RequestSignatureResponse>, IJsonFormatter
	{
		public RequestSignatureResponseFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("success"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("error"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("authToken"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("badgeToken"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("nonce"),
					4
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("success"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("error"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("authToken"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("badgeToken"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("nonce")
			};
		}

		public void Serialize(ref JsonWriter writer, RequestSignatureResponse value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteBoolean(value.success);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.error);
			writer.WriteRaw(this.____stringByteKeys[2]);
			formatterResolver.GetFormatterWithVerify<SignedToken>().Serialize(ref writer, value.authToken, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[3]);
			formatterResolver.GetFormatterWithVerify<SignedToken>().Serialize(ref writer, value.badgeToken, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.nonce);
			writer.WriteEndObject();
		}

		public RequestSignatureResponse Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			bool flag = false;
			string text = null;
			SignedToken signedToken = null;
			SignedToken signedToken2 = null;
			string text2 = null;
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
						text = reader.ReadString();
						break;
					case 2:
						signedToken = formatterResolver.GetFormatterWithVerify<SignedToken>().Deserialize(ref reader, formatterResolver);
						break;
					case 3:
						signedToken2 = formatterResolver.GetFormatterWithVerify<SignedToken>().Deserialize(ref reader, formatterResolver);
						break;
					case 4:
						text2 = reader.ReadString();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new RequestSignatureResponse(flag, text, signedToken, signedToken2, text2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
