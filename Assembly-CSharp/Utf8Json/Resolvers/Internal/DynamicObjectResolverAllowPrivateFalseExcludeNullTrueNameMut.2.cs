using System;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal sealed class DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase : IJsonFormatterResolver
	{
		private DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase()
		{
		}

		public IJsonFormatter<T> GetFormatter<T>()
		{
			return DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.FormatterCache<T>.formatter;
		}

		public static readonly IJsonFormatterResolver Instance = new DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase();

		private static readonly Func<string, string> nameMutator = new Func<string, string>(StringMutator.ToCamelCase);

		private static readonly bool excludeNull = true;

		private const string ModuleName = "Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase");

		private static class FormatterCache<T>
		{
			public static readonly IJsonFormatter<T> formatter = (IJsonFormatter<T>)DynamicObjectTypeBuilder.BuildFormatterToAssembly<T>(DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.assembly, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.Instance, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.nameMutator, DynamicObjectResolverAllowPrivateFalseExcludeNullTrueNameMutateCamelCase.excludeNull);
		}
	}
}
