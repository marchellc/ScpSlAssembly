using System;
using Mirror;

namespace Hints
{
	public class IntHintParameter : PrimitiveHintParameter<int>
	{
		public static IntHintParameter FromNetwork(NetworkReader reader)
		{
			IntHintParameter intHintParameter = new IntHintParameter();
			intHintParameter.Deserialize(reader);
			return intHintParameter;
		}

		protected IntHintParameter()
			: base(new Func<NetworkReader, int>(NetworkReaderExtensions.ReadInt), delegate(NetworkWriter writer, int writerValue)
			{
				writer.WriteInt(writerValue);
			})
		{
		}

		public IntHintParameter(int value)
			: base(value, new Func<NetworkReader, int>(NetworkReaderExtensions.ReadInt), delegate(NetworkWriter writer, int writerValue)
			{
				writer.WriteInt(writerValue);
			})
		{
		}
	}
}
