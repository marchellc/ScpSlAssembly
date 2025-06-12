using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.excludeNull, allowPrivate: true);
		}
	}

	public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase();

	private static readonly Func<string, string> nameMutator = StringMutator.ToSnakeCase;

	private static readonly bool excludeNull = true;

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
