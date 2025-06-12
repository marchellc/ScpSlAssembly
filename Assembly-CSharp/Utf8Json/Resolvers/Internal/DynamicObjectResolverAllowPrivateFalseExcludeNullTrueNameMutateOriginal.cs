using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal;

internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal : IJsonFormatterResolver
{
	private static class FormatterCache<T>
	{
		public static readonly IJsonFormatter<T> formatter;

		static FormatterCache()
		{
			FormatterCache<T>.formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.excludeNull);
		}
	}

	public static readonly IJsonFormatterResolver Instance;

	private static readonly Func<string, string> nameMutator;

	private static readonly bool excludeNull;

	private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal";

	private static readonly DynamicAssembly assembly;

	static DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal()
	{
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal();
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.nameMutator = StringMutator.Original;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.excludeNull = true;
		DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal");
	}

	private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal()
	{
	}

	public IJsonFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.formatter;
	}
}
