using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.excludeNull, allowPrivate: true);
		}
	}

	public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal();

	private static readonly Func<string, string> nameMutator = StringMutator.Original;

	private static readonly bool excludeNull = true;

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
