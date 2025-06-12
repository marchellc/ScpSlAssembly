using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase();
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.nameMutator = StringMutator.ToCamelCase;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.excludeNull = false;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
