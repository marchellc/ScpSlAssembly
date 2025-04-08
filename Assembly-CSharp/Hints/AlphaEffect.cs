using System;
using Mirror;

namespace Hints
{
	public class AlphaEffect : HintEffect
	{
		public static AlphaEffect FromNetwork(NetworkReader reader)
		{
			AlphaEffect alphaEffect = new AlphaEffect();
			alphaEffect.Deserialize(reader);
			return alphaEffect;
		}

		private protected float Alpha { protected get; private set; }

		private AlphaEffect()
			: base(0f, 1f)
		{
		}

		public AlphaEffect(float alpha, float startScalar = 0f, float durationScalar = 1f)
			: base(startScalar, durationScalar)
		{
			this.Alpha = alpha;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.Alpha = reader.ReadFloat();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteFloat(this.Alpha);
		}
	}
}
