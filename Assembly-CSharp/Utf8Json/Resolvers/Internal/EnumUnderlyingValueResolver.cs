using System;
using System.Reflection;
using Utf8Json.Formatters;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class EnumUnderlyingValueResolver : IJsonFormatterResolver
	{
		private EnumUnderlyingValueResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return EnumUnderlyingValueResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new EnumUnderlyingValueResolver();

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				TypeInfo typeInfo = typeof(T).GetTypeInfo();
				if (!typeInfo.IsNullable())
				{
					if (typeof(T).IsEnum)
					{
						EnumUnderlyingValueResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)new EnumFormatter<T>(false);
					}
					return;
				}
				typeInfo = typeInfo.GenericTypeArguments[0].GetTypeInfo();
				if (!typeInfo.IsEnum)
				{
					return;
				}
				object formatterDynamic = EnumUnderlyingValueResolver.Instance.GetFormatterDynamic(typeInfo.AsType());
				if (formatterDynamic == null)
				{
					return;
				}
				EnumUnderlyingValueResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(new Type[] { typeInfo.AsType() }), new object[] { formatterDynamic });
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
