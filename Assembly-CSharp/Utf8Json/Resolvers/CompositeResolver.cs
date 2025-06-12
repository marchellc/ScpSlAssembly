using System;
using System.Reflection;

namespace Utf8Json.Resolvers;

public sealed class CompositeResolver : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			CompositeResolver.isFreezed = true;
			IJsonFormatter[] formatters = CompositeResolver.formatters;
			foreach (IJsonFormatter jsonFormatter in formatters)
			{
				foreach (Type implementedInterface in jsonFormatter.GetType().GetTypeInfo().ImplementedInterfaces)
				{
					TypeInfo typeInfo = implementedInterface.GetTypeInfo();
					if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments[0] == typeof(T))
					{
						FormatterCache<T>.formatter = (IJsonFormatter<T>)jsonFormatter;
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
					FormatterCache<T>.formatter = jsonFormatter2;
					break;
				}
			}
		}
	}

	public static readonly CompositeResolver Instance = new CompositeResolver();

	private static bool isFreezed = false;

	private static IJsonFormatter[] formatters = new IJsonFormatter[0];

	private static IJsonFormatterResolver[] resolvers = new IJsonFormatterResolver[0];

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
		return FormatterCache<T>.formatter;
	}
}
