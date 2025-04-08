using System;
using Mirror;

namespace Hints
{
	public class ByteHintParameter : PrimitiveHintParameter<byte>
	{
		public static ByteHintParameter FromNetwork(NetworkReader reader)
		{
			ByteHintParameter byteHintParameter = new ByteHintParameter();
			byteHintParameter.Deserialize(reader);
			return byteHintParameter;
		}

		protected ByteHintParameter()
			: base(new Func<NetworkReader, byte>(NetworkReaderExtensions.ReadByte), new Action<NetworkWriter, byte>(NetworkWriterExtensions.WriteByte))
		{
		}

		public ByteHintParameter(byte value)
			: base(value, new Func<NetworkReader, byte>(NetworkReaderExtensions.ReadByte), new Action<NetworkWriter, byte>(NetworkWriterExtensions.WriteByte))
		{
		}
	}
}
