using System;

namespace Utf8Json.Resolvers
{
	public class GeneratedResolver : IJsonFormatterResolver
	{
		private GeneratedResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return GeneratedResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new GeneratedResolver();

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				object obj = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
				if (obj != null)
				{
					GeneratedResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)obj;
				}
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
