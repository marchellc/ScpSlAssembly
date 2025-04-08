using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.Original);

		private static readonly bool excludeNull = true;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateOriginal.excludeNull);
		}
	}
}
