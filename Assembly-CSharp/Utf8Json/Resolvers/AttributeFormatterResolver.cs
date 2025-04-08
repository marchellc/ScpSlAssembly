using System;
using System.Reflection;

namespace Utf8Json.Resolvers
{
	public sealed class AttributeFormatterResolver : IJsonFormatterResolver
	{
		private AttributeFormatterResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return AttributeFormatterResolver.FormatterCache<T>.formatter;
		}

		public static IJsonFormatterResolver Instance = new AttributeFormatterResolver();

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				JsonFormatterAttribute customAttribute = typeof(T).GetTypeInfo().GetCustomAttribute<JsonFormatterAttribute>();
				if (customAttribute == null)
				{
					return;
				}
				try
				{
					if (customAttribute.FormatterType.IsGenericType && !customAttribute.FormatterType.GetTypeInfo().IsConstructedGenericType())
					{
						AttributeFormatterResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)Activator.CreateInstance(customAttribute.FormatterType.MakeGenericType(new Type[] { typeof(T) }), customAttribute.Arguments);
					}
					else
					{
						AttributeFormatterResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)Activator.CreateInstance(customAttribute.FormatterType, customAttribute.Arguments);
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Can not create formatter from JsonFormatterAttribute, check the target formatter is public and has constructor with right argument. FormatterType:" + customAttribute.FormatterType.Name, ex);
				}
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
