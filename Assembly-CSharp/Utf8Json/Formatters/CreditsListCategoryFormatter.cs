using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class CreditsListCategoryFormatter : IJsonFormatter<CreditsListCategory>, IJsonFormatter
	{
		public CreditsListCategoryFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("category"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("members"),
					1
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("category"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("members")
			};
		}

		public void Serialize(ref JsonWriter writer, CreditsListCategory value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.category);
			writer.WriteRaw(this.____stringByteKeys[1]);
			formatterResolver.GetFormatterWithVerify<CreditsListMember[]>().Serialize(ref writer, value.members, formatterResolver);
			writer.WriteEndObject();
		}

		public CreditsListCategory Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			CreditsListMember[] array = null;
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
						array = formatterResolver.GetFormatterWithVerify<CreditsListMember[]>().Deserialize(ref reader, formatterResolver);
					}
				}
				else
				{
					text = reader.ReadString();
				}
			}
			return new CreditsListCategory(text, array);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
