using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToCamelCase);

		private static readonly bool excludeNull = false;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullFalseNameMutateCamelCase.excludeNull);
		}
	}
}
