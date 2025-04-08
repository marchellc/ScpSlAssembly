using System;
using System.Reflection;

namespace Utf8Json
{
	public static class JsonFormatterResolverExtensions
	{
		public static IJsonFormatter<T> GetFormatterWithVerify<T>(this IJsonFormatterResolver resolver)
		{
			IJsonFormatter<T> formatter;
			try
			{
				formatter = resolver.GetFormatter<T>();
			}
			catch (TypeInitializationException innerException)
			{
				while (innerException.InnerException != null)
				{
					innerException = innerException.InnerException;
				}
				throw innerException;
			}
			if (formatter == null)
			{
				throw new FormatterNotRegisteredException(typeof(T).FullName + " is not registered in this resolver. resolver:" + resolver.GetType().Name);
			}
			return formatter;
		}

		public static object GetFormatterDynamic(this IJsonFormatterResolver resolver, Type type)
		{
			return typeof(IJsonFormatterResolver).GetRuntimeMethod("GetFormatter", Type.EmptyTypes).MakeGenericMethod(new Type[] { type }).Invoke(resolver, null);
		}

		public static void DeserializeToWithFallbackReplace<T>(this IJsonFormatterResolver formatterResolver, ref T value, ref JsonReader reader)
		{
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			IOverwriteJsonFormatter<T> overwriteJsonFormatter = formatterWithVerify as IOverwriteJsonFormatter<T>;
			if (overwriteJsonFormatter != null)
			{
				overwriteJsonFormatter.DeserializeTo(ref value, ref reader, formatterResolver);
				return;
			}
			value = formatterWithVerify.Deserialize(ref reader, formatterResolver);
		}
	}
}
