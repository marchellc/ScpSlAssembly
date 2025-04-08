using System;
using Mirror;

namespace Hints
{
	public class AmmoHintParameter : IdHintParameter
	{
		public static AmmoHintParameter FromNetwork(NetworkReader reader)
		{
			AmmoHintParameter ammoHintParameter = new AmmoHintParameter();
			ammoHintParameter.Deserialize(reader);
			return ammoHintParameter;
		}

		private AmmoHintParameter()
		{
		}

		public AmmoHintParameter(byte id)
			: base(id)
		{
		}
	}
}
