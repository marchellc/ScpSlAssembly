using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters.Internal
{
	internal static class StandardClassLibraryFormatterHelper
	{
		internal static readonly byte[][] keyValuePairName = new byte[][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("Key"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Value")
		};

		internal static readonly AutomataDictionary keyValuePairAutomata = new AutomataDictionary
		{
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Key"),
				0
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Value"),
				1
			}
		};
	}
}
