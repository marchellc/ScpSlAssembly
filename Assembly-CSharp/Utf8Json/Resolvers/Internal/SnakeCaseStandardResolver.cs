using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class SnakeCaseStandardResolver : IJsonFormatterResolver
	{
		private SnakeCaseStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return SnakeCaseStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new SnakeCaseStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { SnakeCaseStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					SnakeCaseStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)SnakeCaseStandardResolver.fallbackFormatter;
					return;
				}
				SnakeCaseStandardResolver.FormatterCache<T>.formatter = SnakeCaseStandardResolver.InnerResolver.Instance.GetFormatter<T>();
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
				return SnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new SnakeCaseStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.SnakeCase }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = SnakeCaseStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							SnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
