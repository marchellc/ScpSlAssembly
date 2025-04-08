using System;
using CustomRendering;

namespace CustomPlayerEffects
{
	public class FogControl : StatusEffectBase
	{
		public override byte MaxIntensity { get; } = (byte)Enum.GetValues(typeof(FogType)).Length;

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Technical;
			}
		}

		public void SetFogType(FogType fogType)
		{
			base.Intensity = (byte)(fogType + 1);
		}
	}
}
