using System;
using InventorySystem.Items.Usables.Scp330;
using Mirror;

namespace Hints
{
	public class Scp330HintParameter : IdHintParameter
	{
		public static Scp330HintParameter FromNetwork(NetworkReader reader)
		{
			Scp330HintParameter scp330HintParameter = new Scp330HintParameter();
			scp330HintParameter.Deserialize(reader);
			return scp330HintParameter;
		}

		private Scp330HintParameter()
		{
		}

		public Scp330HintParameter(Scp330Translations.Entry index)
			: base((byte)index)
		{
		}
	}
}
