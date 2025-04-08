using System;
using System.Linq;
using Utf8Json.Formatters;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class AllowPrivateStandardResolver : IJsonFormatterResolver
	{
		private AllowPrivateStandardResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return AllowPrivateStandardResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new AllowPrivateStandardResolver();

		private static readonly IJsonFormatter<object> fallbackFormatter = new DynamicObjectTypeFallbackFormatter(new IJsonFormatterResolver[] { AllowPrivateStandardResolver.InnerResolver.Instance });

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				if (typeof(T) == typeof(object))
				{
					AllowPrivateStandardResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)AllowPrivateStandardResolver.fallbackFormatter;
					return;
				}
				AllowPrivateStandardResolver.FormatterCache<T>.formatter = AllowPrivateStandardResolver.InnerResolver.Instance.GetFormatter<T>();
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
				return AllowPrivateStandardResolver.InnerResolver.FormatterCache<T>.formatter;
			}

			public static readonly IJsonFormatterResolver Instance = new AllowPrivateStandardResolver.InnerResolver();

			private static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase.Concat(new IJsonFormatterResolver[] { DynamicObjectResolver.AllowPrivate }).ToArray<IJsonFormatterResolver>();

			private static class FormatterCache<T>
			{
				static FormatterCache()
				{
					IJsonFormatterResolver[] resolvers = AllowPrivateStandardResolver.InnerResolver.resolvers;
					for (int i = 0; i < resolvers.Length; i++)
					{
						IJsonFormatter<T> jsonFormatter = resolvers[i].GetFormatter<T>();
						if (jsonFormatter != null)
						{
							AllowPrivateStandardResolver.InnerResolver.FormatterCache<T>.formatter = jsonFormatter;
							return;
						}
					}
				}

				public static readonly IJsonFormatter<T> formatter;
			}
		}
	}
}
