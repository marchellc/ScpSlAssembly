using System;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase : IJsonFormatterResolver
	{
		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToSnakeCase);

		private static readonly bool excludeNull = true;

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToDynamicMethod<T>(DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateTrueExcludeNullTrueNameMutateSnakeCase.excludeNull, true);
		}
	}
}
