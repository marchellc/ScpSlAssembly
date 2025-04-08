using System;
using System.Reflection;
using Utf8Json.Formatters;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class EnumDefaultResolver : IJsonFormatterResolver
	{
		private EnumDefaultResolver()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return EnumDefaultResolver.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new EnumDefaultResolver();

		private static class FormatterCache<T>
		{
			static FormatterCache()
			{
				TypeInfo typeInfo = typeof(T).GetTypeInfo();
				if (!typeInfo.IsNullable())
				{
					if (typeof(T).IsEnum)
					{
						EnumDefaultResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)new EnumFormatter<T>(true);
					}
					return;
				}
				typeInfo = typeInfo.GenericTypeArguments[0].GetTypeInfo();
				if (!typeInfo.IsEnum)
				{
					return;
				}
				object formatterDynamic = EnumDefaultResolver.Instance.GetFormatterDynamic(typeInfo.AsType());
				if (formatterDynamic == null)
				{
					return;
				}
				EnumDefaultResolver.FormatterCache<T>.formatter = (IJsonFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(new Type[] { typeInfo.AsType() }), new object[] { formatterDynamic });
			}

			public static readonly IJsonFormatter<T> formatter;
		}
	}
}
