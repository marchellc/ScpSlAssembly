using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.excludeNull, allowPrivate: true);
		}
	}

	public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal();

	private static readonly Func<string, string> nameMutator = StringMutator.Original;

	private static readonly bool excludeNull = false;

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
