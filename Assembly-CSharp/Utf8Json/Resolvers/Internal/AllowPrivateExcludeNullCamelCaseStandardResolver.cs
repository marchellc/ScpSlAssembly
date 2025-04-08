using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class AllowPrivateExcludeNullCamelCaseStandardResolver : IJsonFormatterResolver
	{
		private AllowPrivateExcludeNullCamelCaseStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return AllowPrivateExcludeNullCamelCaseStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullCamelCaseStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					AllowPrivateExcludeNullCamelCaseStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)AllowPrivateExcludeNullCamelCaseStandardResolver.fallbackFormatter;
					return;
				}
				AllowPrivateExcludeNullCamelCaseStandardResolver.FormatterCache<T>.formatter = AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver.Instance.GetFormatter<T>();
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
				return AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.AllowPrivateExcludeNullCamelCase }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							AllowPrivateExcludeNullCamelCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
