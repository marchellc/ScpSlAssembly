using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase.excludeNull, allowPrivate: true);
		}
	}

	public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateSnakeCase();

	private static readonly Func<string, string> nameMutator = StringMutator.ToSnakeCase;

	private static readonly bool excludeNull = false;

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
