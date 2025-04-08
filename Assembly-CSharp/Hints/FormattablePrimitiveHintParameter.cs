using System;
using Mirror;

namespace Hints
{
	public abstract class FormattablePrimitiveHintParameter<TValue> : PrimitiveHintParameter<TValue>
	{
		private protected string Format { protected get; private set; }

		protected FormattablePrimitiveHintParameter(Func<NetworkReader, TValue> deserializer, Action<NetworkWriter, TValue> serializer)
			: base(deserializer, serializer)
		{
		}

		protected FormattablePrimitiveHintParameter(TValue value, string format, Func<NetworkReader, TValue> deserializer, Action<NetworkWriter, TValue> serializer)
			: base(value, deserializer, serializer)
		{
			this.Format = format;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.Format = reader.ReadString();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteString(this.Format);
		}
	}
}
