using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToSnakeCase);

		private static readonly bool excludeNull = false;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.excludeNull, true);
		}
	}
}
