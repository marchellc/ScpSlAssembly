using System;
using Mirror;

namespace Hints
{
	public class SByteHintParameter : PrimitiveHintParameter<sbyte>
	{
		public static SByteHintParameter FromNetwork(NetworkReader reader)
		{
			SByteHintParameter sbyteHintParameter = new SByteHintParameter();
			sbyteHintParameter.Deserialize(reader);
			return sbyteHintParameter;
		}

		protected SByteHintParameter()
			: base(new Func<NetworkReader, sbyte>(NetworkReaderExtensions.ReadSByte), new Action<NetworkWriter, sbyte>(NetworkWriterExtensions.WriteSByte))
		{
		}

		public SByteHintParameter(sbyte value)
			: base(value, new Func<NetworkReader, sbyte>(NetworkReaderExtensions.ReadSByte), new Action<NetworkWriter, sbyte>(NetworkWriterExtensions.WriteSByte))
		{
		}
	}
}
