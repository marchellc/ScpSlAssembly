using System;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Serialization
{
	public static class YamlParser
	{
		public static ISerializer Serializer { get; } = new SerializerBuilder().WithEmissionPhaseObjectGraphVisitor<CommentsObjectGraphVisitor>(([Nullable(1)] EmissionPhaseObjectGraphVisitorArgs visitor) => new CommentsObjectGraphVisitor(visitor.InnerVisitor)).WithTypeInspector<CommentGatheringTypeInspector>(([Nullable(1)] ITypeInspector typeInspector) => new CommentGatheringTypeInspector(typeInspector)).WithNamingConvention(UnderscoredNamingConvention.Instance)
			.DisableAliases()
			.IgnoreFields()
			.Build();

		public static IDeserializer Deserializer { get; } = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().IgnoreFields()
			.Build();
	}
}
