using System;
using Mirror;

namespace Hints
{
	public class UShortHintParameter : PrimitiveHintParameter<ushort>
	{
		public static UShortHintParameter FromNetwork(NetworkReader reader)
		{
			UShortHintParameter ushortHintParameter = new UShortHintParameter();
			ushortHintParameter.Deserialize(reader);
			return ushortHintParameter;
		}

		protected UShortHintParameter()
			: base(new Func<NetworkReader, ushort>(NetworkReaderExtensions.ReadUShort), new Action<NetworkWriter, ushort>(NetworkWriterExtensions.WriteUShort))
		{
		}

		public UShortHintParameter(ushort value)
			: base(value, new Func<NetworkReader, ushort>(NetworkReaderExtensions.ReadUShort), new Action<NetworkWriter, ushort>(NetworkWriterExtensions.WriteUShort))
		{
		}
	}
}
