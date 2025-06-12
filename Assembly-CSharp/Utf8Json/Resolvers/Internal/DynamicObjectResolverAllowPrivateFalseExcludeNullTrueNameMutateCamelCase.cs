using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase();
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.nameMutator = StringMutator.ToCamelCase;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.excludeNull = true;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
