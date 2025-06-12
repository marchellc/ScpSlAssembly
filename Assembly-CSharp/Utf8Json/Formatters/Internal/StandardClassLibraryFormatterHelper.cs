using Utf8Json.Internal;

namespace Utf8Json.Formatters.Internal;

internal static class StandardClassLibraryFormatterHelper
{
	internal static readonly byte[][] keyValuePairName;

	internal static readonly AutomataDictionary keyValuePairAutomata;

	static StandardClassLibraryFormatterHelper()
	{
		StandardClassLibraryFormatterHelper.keyValuePairName = new byte[2][]
		{
			JsonWriter.GetEncodedPropertyNameWithBeginObject("Key"),
			JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Value")
		};
		StandardClassLibraryFormatterHelper.keyValuePairAutomata = new AutomataDictionary
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
