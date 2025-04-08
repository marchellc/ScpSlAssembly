using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToCamelCase);

		private static readonly bool excludeNull = false;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateCamelCase.excludeNull, true);
		}
	}
}
