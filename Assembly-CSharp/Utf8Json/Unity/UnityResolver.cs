using System;

namespace Utf8Json.Unity
{
	public class UnityResolver : IJsonFormatterResolver
	{
		private UnityResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return UnityResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new UnityResolver();

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				object obj = UnityResolverGetFormatterHelper.GetFormatter(typeof(T));
				if (obj != null)
				{
					UnityResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)obj;
				}
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
