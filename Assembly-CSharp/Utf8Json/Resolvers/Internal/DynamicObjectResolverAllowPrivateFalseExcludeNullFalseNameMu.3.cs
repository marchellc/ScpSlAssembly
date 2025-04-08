using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToSnakeCase);

		private static readonly bool excludeNull = false;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.excludeNull);
		}
	}
}
