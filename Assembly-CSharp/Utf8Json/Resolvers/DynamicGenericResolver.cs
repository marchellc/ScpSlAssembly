using System;
using Utf8Json.Resolvers.Internal;

namespace Utf8Json.Resolvers
{
	public sealed class DynamicGenericResolver : IJsonFormatterResolver
	{
		private DynamicGenericResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicGenericResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicGenericResolver();

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicGenericResolverGetFormatterHelper.GetFormatter(typeof(T));
		}
	}
}
