using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class AllowPrivateSnakeCaseStandardResolver : IJsonFormatterResolver
	{
		private AllowPrivateSnakeCaseStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return AllowPrivateSnakeCaseStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new AllowPrivateSnakeCaseStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { AllowPrivateSnakeCaseStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					AllowPrivateSnakeCaseStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)AllowPrivateSnakeCaseStandardResolver.fallbackFormatter;
					return;
				}
				AllowPrivateSnakeCaseStandardResolver.FormatterCache<T>.formatter = AllowPrivateSnakeCaseStandardResolver.InnerResolver.Instance.GetFormatter<T>();
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
				return AllowPrivateSnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new AllowPrivateSnakeCaseStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.AllowPrivateSnakeCase }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = AllowPrivateSnakeCaseStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							AllowPrivateSnakeCaseStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
