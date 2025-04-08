using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.Original);

		private static readonly bool excludeNull = true;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateOriginal.excludeNull, true);
		}
	}
}
