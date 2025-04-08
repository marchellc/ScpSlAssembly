using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.Original);

		private static readonly bool excludeNull = false;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullFalseNameMutateOriginal.excludeNull, true);
		}
	}
}
