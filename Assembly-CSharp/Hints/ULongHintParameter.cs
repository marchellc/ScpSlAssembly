using System;
using Mirror;

namespace Hints
{
	public class ULongHintParameter : PrimitiveHintParameter<ulong>
	{
		public static ULongHintParameter FromNetwork(NetworkReader reader)
		{
			ULongHintParameter ulongHintParameter = new ULongHintParameter();
			ulongHintParameter.Deserialize(reader);
			return ulongHintParameter;
		}

		protected ULongHintParameter()
			: base(new Func<NetworkReader, ulong>(NetworkReaderExtensions.ReadULong), delegate(NetworkWriter writer, ulong writerValue)
			{
				writer.WriteULong(writerValue);
			})
		{
		}

		public ULongHintParameter(ulong value)
			: base(value, new Func<NetworkReader, ulong>(NetworkReaderExtensions.ReadULong), delegate(NetworkWriter writer, ulong writerValue)
			{
				writer.WriteULong(writerValue);
			})
		{
		}
	}
}
