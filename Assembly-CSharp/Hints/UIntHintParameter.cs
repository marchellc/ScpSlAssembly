using System;
using Mirror;

namespace Hints
{
	public class UIntHintParameter : PrimitiveHintParameter<uint>
	{
		public static UIntHintParameter FromNetwork(NetworkReader reader)
		{
			UIntHintParameter uintHintParameter = new UIntHintParameter();
			uintHintParameter.Deserialize(reader);
			return uintHintParameter;
		}

		protected UIntHintParameter()
			: base(new Func<NetworkReader, uint>(NetworkReaderExtensions.ReadUInt), delegate(NetworkWriter writer, uint writerValue)
			{
				writer.WriteUInt(writerValue);
			})
		{
		}

		public UIntHintParameter(uint value)
			: base(value, new Func<NetworkReader, uint>(NetworkReaderExtensions.ReadUInt), delegate(NetworkWriter writer, uint writerValue)
			{
				writer.WriteUInt(writerValue);
			})
		{
		}
	}
}
