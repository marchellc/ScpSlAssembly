using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase();
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.nameMutator = StringMutator.ToSnakeCase;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.excludeNull = true;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateSnakeCase()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
