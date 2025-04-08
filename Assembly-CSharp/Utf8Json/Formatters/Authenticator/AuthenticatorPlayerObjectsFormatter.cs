using System;
using System.Collections.Generic;
using Authenticator;
using Utf8Json.Internal;

namespace Utf8Json.Formatters.Authenticator
{
	public sealed class AuthenticatorPlayerObjectsFormatter : IJsonFormatter<AuthenticatorPlayerObjects>, IJsonFormatter
	{
		public AuthenticatorPlayerObjectsFormatter()
		{
			this.____keyMapping = new AutomataDictionary { 
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("objects"),
				0
			} };
			this.____stringByteKeys = new byte[][] { JsonWriter.GetEncodedPropertyNameWithBeginObject("objects") };
		}

		public void Serialize(ref JsonWriter writer, AuthenticatorPlayerObjects value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<List<AuthenticatorPlayerObject>>().Serialize(ref writer, value.objects, formatterResolver);
			writer.WriteEndObject();
		}

		public AuthenticatorPlayerObjects Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			List<AuthenticatorPlayerObject> list = null;
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
				else if (num2 == 0)
				{
					list = formatterResolver.GetFormatterWithVerify<List<AuthenticatorPlayerObject>>().Deserialize(ref reader, formatterResolver);
				}
				else
				{
					reader.ReadNextBlock();
				}
			}
			return new AuthenticatorPlayerObjects(list);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
