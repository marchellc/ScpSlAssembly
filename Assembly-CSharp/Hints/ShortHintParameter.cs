using System;
using Mirror;

namespace Hints
{
	public class ShortHintParameter : PrimitiveHintParameter<short>
	{
		public static ShortHintParameter FromNetwork(NetworkReader reader)
		{
			ShortHintParameter shortHintParameter = new ShortHintParameter();
			shortHintParameter.Deserialize(reader);
			return shortHintParameter;
		}

		protected ShortHintParameter()
			: base(new Func<NetworkReader, short>(NetworkReaderExtensions.ReadShort), new Action<NetworkWriter, short>(NetworkWriterExtensions.WriteShort))
		{
		}

		public ShortHintParameter(short value)
			: base(value, new Func<NetworkReader, short>(NetworkReaderExtensions.ReadShort), new Action<NetworkWriter, short>(NetworkWriterExtensions.WriteShort))
		{
		}
	}
}
