using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal();
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.nameMutator = StringMutator.Original;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.excludeNull = false;
		DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
