using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToCamelCase);

		private static readonly bool excludeNull = true;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateCamelCase.excludeNull, true);
		}
	}
}
