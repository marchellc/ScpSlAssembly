using System;
using Mirror;
using Utils.Networking;

namespace Hints
{
	public abstract class Hint : DisplayableObject<SharedHintData>
	{
		private protected HintParameter[] Parameters { protected get; private set; }

		protected Hint(HintParameter[] parameters, HintEffect[] effects, float durationScalar = 1f)
			: base(durationScalar)
		{
			this._effects = effects;
			this.Parameters = parameters;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this._effects = reader.ReadHintEffectArray();
			this.Parameters = reader.ReadHintParameterArray();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteHintEffectArray(this._effects);
			writer.WriteHintParameterArray(this.Parameters);
		}

		private HintEffect[] _effects;
	}
}
