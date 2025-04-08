using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class EmotionRenderer : MonoBehaviour
	{
		public EmotionBlendshape[] Blendshapes { get; private set; }

		private void Awake()
		{
			this._rend = base.GetComponent<SkinnedMeshRenderer>();
			this._blendshapeToIndex = new int?[EnumUtils<EmotionBlendshape>.Values.Length];
			for (int i = 0; i < this.Blendshapes.Length; i++)
			{
				this._blendshapeToIndex[(int)this.Blendshapes[i]] = new int?(i);
			}
		}

		public float GetWeight(EmotionBlendshape blendshape)
		{
			int? num = this._blendshapeToIndex[(int)blendshape];
			if (num == null)
			{
				return 0f;
			}
			return this._rend.GetBlendShapeWeight(num.Value) * 100f;
		}

		public void SetWeight(EmotionBlendshape blendshape, float weight)
		{
			int? num = this._blendshapeToIndex[(int)blendshape];
			if (num != null)
			{
				this._rend.SetBlendShapeWeight(num.Value, weight * 100f);
			}
		}

		public void ResetWeights()
		{
			foreach (EmotionBlendshape emotionBlendshape in this.Blendshapes)
			{
				this.SetWeight(emotionBlendshape, 0f);
			}
		}

		private SkinnedMeshRenderer _rend;

		private int?[] _blendshapeToIndex;
	}
}
