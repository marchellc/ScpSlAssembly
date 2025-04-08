using System;
using System.Reflection;

namespace Utf8Json.Resolvers
{
	public sealed class CompositeResolver : IJsonFormatterResolver
	{
		private CompositeResolver()
		{
		}

		public static void Register(params IJsonFormatterResolver[] resolvers)
		{
			if (CompositeResolver.isFreezed)
			{
				throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
			}
			CompositeResolver.resolvers = resolvers;
		}

		public static void Register(params IJsonFormatter[] formatters)
		{
			if (CompositeResolver.isFreezed)
			{
				throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
			}
			CompositeResolver.formatters = formatters;
		}

		public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
		{
			if (CompositeResolver.isFreezed)
			{
				throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
			}
			CompositeResolver.resolvers = resolvers;
			CompositeResolver.formatters = formatters;
		}

		public static void RegisterAndSetAsDefault(params IJsonFormatterResolver[] resolvers)
		{
			CompositeResolver.Register(resolvers);
			JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
		}

		public static void RegisterAndSetAsDefault(params IJsonFormatter[] formatters)
		{
			CompositeResolver.Register(formatters);
			JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
		}

		public static void RegisterAndSetAsDefault(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
		{
			CompositeResolver.Register(formatters);
			CompositeResolver.Register(resolvers);
			JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
		}

		public static IJsonFormatterResolver Create(params IJsonFormatter[] formatters)
		{
			return CompositeResolver.Create(formatters, new IJsonFormatterResolver[0]);
		}

		public static IJsonFormatterResolver Create(params IJsonFormatterResolver[] resolvers)
		{
			return CompositeResolver.Create(new IJsonFormatter[0], resolvers);
		}

		public static IJsonFormatterResolver Create(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
		{
			return DynamicCompositeResolver.Create(formatters, resolvers);
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return CompositeResolver.FormatterCache<T>.formatter;
		}

		public static readonly CompositeResolver Instance = new CompositeResolver();

		private static bool isFreezed = false;

		private static IJsonFormatter[] formatters = new IJsonFormatter[0];

		private static IJsonFormatterResolver[] resolvers = new IJsonFormatterResolver[0];

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				CompositeResolver.isFreezed = true;
				foreach (IJsonFormatter jsonFormatter in CompositeResolver.formatters)
				{
					foreach (Type type in jsonFormatter.GetType().GetTypeInfo().ImplementedInterfaces)
					{
						TypeInfo typeInfo = type.GetTypeInfo();
						if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments[0] == typeof(T))
						{
							CompositeResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)jsonFormatter;
							return;
						}
					}
				}
				IJsonFormatterResolver[] resolvers = CompositeResolver.resolvers;
				for (int i = 0; i < resolvers.Length; i++)
				{
					IJsonFormatter<T> jsonFormatter2 = resolvers[i].GetFormatter<T>();
					if (jsonFormatter2 != null)
					{
						CompositeResolver.FormatterCache<T>.formatter = jsonFormatter2;
						return;
					}
				}
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
