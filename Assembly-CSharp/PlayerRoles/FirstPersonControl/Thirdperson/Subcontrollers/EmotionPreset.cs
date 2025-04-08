using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	[Serializable]
	public struct EmotionPreset
	{
		public readonly float GetWeight(EmotionBlendshape blendshape)
		{
			foreach (EmotionPreset.BlendshapeWeightPair blendshapeWeightPair in this.Pairs)
			{
				if (blendshapeWeightPair.Blendshape == blendshape)
				{
					return blendshapeWeightPair.Weight;
				}
			}
			return 0f;
		}

		public readonly void SetWeights(Action<EmotionBlendshape, float> setter)
		{
			foreach (EmotionPreset.BlendshapeWeightPair blendshapeWeightPair in this.Pairs)
			{
				setter(blendshapeWeightPair.Blendshape, blendshapeWeightPair.Weight);
			}
		}

		public EmotionPresetType PresetType;

		public EmotionPreset.BlendshapeWeightPair[] Pairs;

		[Serializable]
		public struct BlendshapeWeightPair
		{
			public EmotionBlendshape Blendshape;

			[Range(0f, 1f)]
			public float Weight;
		}
	}
}
