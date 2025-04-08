using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class AllowPrivateExcludeNullSnakeCaseStandardResolver : IJsonFormatterResolver
	{
		private AllowPrivateExcludeNullSnakeCaseStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return AllowPrivateExcludeNullSnakeCaseStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullSnakeCaseStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					AllowPrivateExcludeNullSnakeCaseStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)AllowPrivateExcludeNullSnakeCaseStandardResolver.fallbackFormatter;
					return;
				}
				AllowPrivateExcludeNullSnakeCaseStandardResolver.FormatterCache<T>.formatter = AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver.Instance.GetFormatter<T>();
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
				return AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.AllowPrivateExcludeNullSnakeCase }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							AllowPrivateExcludeNullSnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
