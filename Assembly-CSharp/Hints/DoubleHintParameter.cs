using System;
using Mirror;

namespace Hints
{
	public class DoubleHintParameter : FormattablePrimitiveHintParameter<double>
	{
		public static DoubleHintParameter FromNetwork(NetworkReader reader)
		{
			DoubleHintParameter doubleHintParameter = new DoubleHintParameter();
			doubleHintParameter.Deserialize(reader);
			return doubleHintParameter;
		}

		protected DoubleHintParameter()
			: base(new Func<NetworkReader, double>(NetworkReaderExtensions.ReadDouble), new Action<NetworkWriter, double>(NetworkWriterExtensions.WriteDouble))
		{
		}

		public DoubleHintParameter(double value, string format)
			: base(value, format, new Func<NetworkReader, double>(NetworkReaderExtensions.ReadDouble), new Action<NetworkWriter, double>(NetworkWriterExtensions.WriteDouble))
		{
		}
	}
}
