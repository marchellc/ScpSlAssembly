using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters.Internal
{
	internal static class CollectionFormatterHelper
	{
		internal static readonly byte[][] groupingName = new byte[][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("Key"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Elements")
		};

		internal static readonly AutomataDictionary groupingAutomata = new AutomataDictionary
		{
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Key"),
				0
			},
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("Elements"),
				1
			}
		};
	}
}
