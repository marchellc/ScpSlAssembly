using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase();
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.nameMutator = StringMutator.ToSnakeCase;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.excludeNull = false;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateSnakeCase()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
