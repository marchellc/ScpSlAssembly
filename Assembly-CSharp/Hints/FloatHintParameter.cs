using System;
using Mirror;

namespace Hints
{
	public class FloatHintParameter : FormattablePrimitiveHintParameter<float>
	{
		public static FloatHintParameter FromNetwork(NetworkReader reader)
		{
			FloatHintParameter floatHintParameter = new FloatHintParameter();
			floatHintParameter.Deserialize(reader);
			return floatHintParameter;
		}

		protected FloatHintParameter()
			: base(new Func<NetworkReader, float>(NetworkReaderExtensions.ReadFloat), new Action<NetworkWriter, float>(NetworkWriterExtensions.WriteFloat))
		{
		}

		public FloatHintParameter(float value, string format)
			: base(value, format, new Func<NetworkReader, float>(NetworkReaderExtensions.ReadFloat), new Action<NetworkWriter, float>(NetworkWriterExtensions.WriteFloat))
		{
		}
	}
}
