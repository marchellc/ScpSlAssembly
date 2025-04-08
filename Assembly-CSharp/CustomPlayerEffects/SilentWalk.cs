using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class SilentWalk : StatusEffectBase, IFootstepEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public float ProcessFootstepOverrides(float dis)
		{
			return Mathf.Max(1f - 0.1f * (float)base.Intensity, 0f);
		}

		private const float VolumePerStack = 0.1f;
	}
}
