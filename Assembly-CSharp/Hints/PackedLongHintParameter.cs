using System;
using Mirror;

namespace Hints
{
	public class PackedLongHintParameter : PrimitiveHintParameter<long>
	{
		public static PackedLongHintParameter FromNetwork(NetworkReader reader)
		{
			PackedLongHintParameter packedLongHintParameter = new PackedLongHintParameter();
			packedLongHintParameter.Deserialize(reader);
			return packedLongHintParameter;
		}

		protected PackedLongHintParameter()
			: base(new Func<NetworkReader, long>(NetworkReaderExtensions.ReadLong), new Action<NetworkWriter, long>(NetworkWriterExtensions.WriteLong))
		{
		}

		public PackedLongHintParameter(long value)
			: base(value, new Func<NetworkReader, long>(NetworkReaderExtensions.ReadLong), new Action<NetworkWriter, long>(NetworkWriterExtensions.WriteLong))
		{
		}
	}
}
