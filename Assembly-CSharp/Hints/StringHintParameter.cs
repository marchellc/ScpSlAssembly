using System;
using Mirror;

namespace Hints
{
	public class StringHintParameter : PrimitiveHintParameter<string>
	{
		public static StringHintParameter FromNetwork(NetworkReader reader)
		{
			StringHintParameter stringHintParameter = new StringHintParameter();
			stringHintParameter.Deserialize(reader);
			return stringHintParameter;
		}

		protected StringHintParameter()
			: base(new Func<NetworkReader, string>(NetworkReaderExtensions.ReadString), new Action<NetworkWriter, string>(NetworkWriterExtensions.WriteString))
		{
		}

		public StringHintParameter(string value)
			: base(value, new Func<NetworkReader, string>(NetworkReaderExtensions.ReadString), new Action<NetworkWriter, string>(NetworkWriterExtensions.WriteString))
		{
		}
	}
}
