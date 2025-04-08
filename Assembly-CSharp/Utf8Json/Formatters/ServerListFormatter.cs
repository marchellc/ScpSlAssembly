using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ServerListFormatter : IJsonFormatter<ServerList>, IJsonFormatter
	{
		public ServerListFormatter()
		{
			this.____keyMapping = new AutomataDictionary { 
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("servers"),
				0
			} };
			this.____stringByteKeys = new byte[][] { JsonWriter.GetEncodedPropertyNameWithBeginObject("servers") };
		}

		public void Serialize(ref JsonWriter writer, ServerList value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<ServerListItem[]>().Serialize(ref writer, value.servers, formatterResolver);
			writer.WriteEndObject();
		}

		public ServerList Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			ServerListItem[] array = null;
			int num = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num2))
				{
					reader.ReadNextBlock();
				}
				else if (num2 == 0)
				{
					array = formatterResolver.GetFormatterWithVerify<ServerListItem[]>().Deserialize(ref reader, formatterResolver);
				}
				else
				{
					reader.ReadNextBlock();
				}
			}
			return new ServerList(array);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
