using System;
using Mirror;

namespace Hints
{
	public class PackedULongHintParameter : PrimitiveHintParameter<ulong>
	{
		public static PackedULongHintParameter FromNetwork(NetworkReader reader)
		{
			PackedULongHintParameter packedULongHintParameter = new PackedULongHintParameter();
			packedULongHintParameter.Deserialize(reader);
			return packedULongHintParameter;
		}

		protected PackedULongHintParameter()
			: base(new Func<NetworkReader, ulong>(NetworkReaderExtensions.ReadULong), new Action<NetworkWriter, ulong>(NetworkWriterExtensions.WriteULong))
		{
		}

		public PackedULongHintParameter(ulong value)
			: base(value, new Func<NetworkReader, ulong>(NetworkReaderExtensions.ReadULong), new Action<NetworkWriter, ulong>(NetworkWriterExtensions.WriteULong))
		{
		}
	}
}
