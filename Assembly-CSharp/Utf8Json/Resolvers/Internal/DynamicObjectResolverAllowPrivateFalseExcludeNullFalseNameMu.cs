using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.Original);

		private static readonly bool excludeNull = false;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateOriginal.excludeNull);
		}
	}
}
