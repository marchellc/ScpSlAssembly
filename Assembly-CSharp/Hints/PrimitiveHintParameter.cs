using System;
using Mirror;

namespace Hints
{
	public abstract class PrimitiveHintParameter<TValue> : HintParameter
	{
		private protected TValue Value { protected get; private set; }

		protected PrimitiveHintParameter(Func<NetworkReader, TValue> deserializer, Action<NetworkWriter, TValue> serializer)
		{
			this._deserializer = deserializer;
			this._serializer = serializer;
		}

		protected PrimitiveHintParameter(TValue value, Func<NetworkReader, TValue> deserializer, Action<NetworkWriter, TValue> serializer)
			: this(deserializer, serializer)
		{
			this.Value = value;
		}

		public override void Deserialize(NetworkReader reader)
		{
			this.Value = this._deserializer(reader);
		}

		public override void Serialize(NetworkWriter writer)
		{
			this._serializer(writer, this.Value);
		}

		private readonly Func<NetworkReader, TValue> _deserializer;

		private readonly Action<NetworkWriter, TValue> _serializer;

		private bool _stopFormatting;
	}
}
