using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class CreditsListFormatter : IJsonFormatter<CreditsList>, IJsonFormatter
	{
		public CreditsListFormatter()
		{
			this.____keyMapping = new AutomataDictionary { 
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("credits"),
				0
			} };
			this.____stringByteKeys = new byte[][] { JsonWriter.GetEncodedPropertyNameWithBeginObject("credits") };
		}

		public void Serialize(ref JsonWriter writer, CreditsList value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<CreditsListCategory[]>().Serialize(ref writer, value.credits, formatterResolver);
			writer.WriteEndObject();
		}

		public CreditsList Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			CreditsListCategory[] array = null;
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
					array = formatterResolver.GetFormatterWithVerify<CreditsListCategory[]>().Deserialize(ref reader, formatterResolver);
				}
				else
				{
					reader.ReadNextBlock();
				}
			}
			return new CreditsList(array);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
