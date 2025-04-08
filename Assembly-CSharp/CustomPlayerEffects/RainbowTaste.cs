using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class RainbowTaste : StatusEffectBase, ISpectatorDataPlayerEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = "Rainbow Taste";
			return base.IsEnabled;
		}

		public static float CurrentMultiplier(ReferenceHub ply)
		{
			byte intensity = ply.playerEffectsController.GetEffect<RainbowTaste>().Intensity;
			return RainbowTaste.Multipliers[Mathf.Clamp((int)intensity, 0, RainbowTaste.Multipliers.Length - 1)];
		}

		public static bool CheckPlayer(ReferenceHub ply)
		{
			return ply.playerEffectsController.GetEffect<RainbowTaste>().Intensity > 0;
		}

		private static readonly float[] Multipliers = new float[] { 1f, 0.6f, 0.4f, 0.35f };
	}
}
