using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class TranslationManifestFormatter : IJsonFormatter<TranslationManifest>, IJsonFormatter
	{
		public TranslationManifestFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Name"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Authors"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("InterfaceLocales"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("SystemLocales"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("ForcedFontOrder"),
					4
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("Name"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Authors"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("InterfaceLocales"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("SystemLocales"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("ForcedFontOrder")
			};
		}

		public void Serialize(ref JsonWriter writer, TranslationManifest value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.Name);
			writer.WriteRaw(this.____stringByteKeys[1]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.Authors, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[2]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.InterfaceLocales, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[3]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.SystemLocales, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[4]);
			formatterResolver.GetFormatterWithVerify<string[]>().Serialize(ref writer, value.ForcedFontOrder, formatterResolver);
			writer.WriteEndObject();
		}

		public TranslationManifest Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string[] array = null;
			string[] array2 = null;
			string[] array3 = null;
			string[] array4 = null;
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
						text = reader.ReadString();
						break;
					case 1:
						array = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 2:
						array2 = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 3:
						array3 = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 4:
						array4 = formatterResolver.GetFormatterWithVerify<string[]>().Deserialize(ref reader, formatterResolver);
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new TranslationManifest(text, array, array2, array3, array4);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
