using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToSnakeCase);

		private static readonly bool excludeNull = true;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.excludeNull);
		}
	}
}
