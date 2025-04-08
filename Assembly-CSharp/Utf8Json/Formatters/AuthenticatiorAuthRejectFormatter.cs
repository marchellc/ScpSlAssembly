using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class AuthenticatiorAuthRejectFormatter : IJsonFormatter<AuthenticatiorAuthReject>, IJsonFormatter
	{
		public AuthenticatiorAuthRejectFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Id"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Reason"),
					1
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("Id"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Reason")
			};
		}

		public void Serialize(ref JsonWriter writer, AuthenticatiorAuthReject value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.Id);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.Reason);
			writer.WriteEndObject();
		}

		public AuthenticatiorAuthReject Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
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
				else if (num2 != 0)
				{
					if (num2 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						text2 = reader.ReadString();
					}
				}
				else
				{
					text = reader.ReadString();
				}
			}
			return new AuthenticatiorAuthReject(text, text2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
