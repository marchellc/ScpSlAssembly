using System;
using Mirror;

namespace Hints
{
	public class LongHintParameter : PrimitiveHintParameter<long>
	{
		public static LongHintParameter FromNetwork(NetworkReader reader)
		{
			LongHintParameter longHintParameter = new LongHintParameter();
			longHintParameter.Deserialize(reader);
			return longHintParameter;
		}

		protected LongHintParameter()
			: base(new Func<NetworkReader, long>(NetworkReaderExtensions.ReadLong), delegate(NetworkWriter writer, long writerValue)
			{
				writer.WriteLong(writerValue);
			})
		{
		}

		public LongHintParameter(long value)
			: base(value, new Func<NetworkReader, long>(NetworkReaderExtensions.ReadLong), delegate(NetworkWriter writer, long writerValue)
			{
				writer.WriteLong(writerValue);
			})
		{
		}
	}
}
