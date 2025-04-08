using System;

namespace Utf8Json
{
	public enum JsonToken : byte
	{
		None,
		BeginObject,
		EndObject,
		BeginArray,
		EndArray,
		Number,
		String,
		True,
		False,
		Null,
		ValueSeparator,
		NameSeparator
	}
}
