using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class CamelCaseStandardResolver : IJsonFormatterResolver
	{
		private CamelCaseStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return CamelCaseStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new CamelCaseStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { CamelCaseStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					CamelCaseStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)CamelCaseStandardResolver.fallbackFormatter;
					return;
				}
				CamelCaseStandardResolver.FormatterCache<T>.formatter = CamelCaseStandardResolver.InnerResolver.Instance.GetFormatter<T>();
			}

			public static readonly IJsonFormatter<T> formatter;
		}

		private sealed class InnerResolver : IJsonFormatterResolver
		{
			private InnerResolver()
			{
			}

			public IJsonFormatter<T> GetFormatter<T>()
			{
				return CamelCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new CamelCaseStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.CamelCase }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = CamelCaseStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							CamelCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
