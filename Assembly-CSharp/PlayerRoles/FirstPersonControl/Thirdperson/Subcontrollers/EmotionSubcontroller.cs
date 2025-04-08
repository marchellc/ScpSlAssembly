using System;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class EmotionSubcontroller : SubcontrollerBehaviour, IPoolResettable
	{
		public EmotionRenderer[] Renderers { get; private set; }

		public EmotionPreset[] Presets { get; private set; }

		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			this._rendCnt = this.Renderers.Length;
		}

		public override void OnReassigned()
		{
			base.OnReassigned();
			this.SetPreset(EmotionSync.GetEmotionPreset(base.OwnerHub));
		}

		public void ResetObject()
		{
			this.ResetWeights();
		}

		public void SetPreset(EmotionPresetType presetType)
		{
			this.ResetWeights();
			EmotionPreset? emotionPreset = null;
			foreach (EmotionPreset emotionPreset2 in this.Presets)
			{
				if (emotionPreset2.PresetType == presetType)
				{
					emotionPreset2.SetWeights(new Action<EmotionBlendshape, float>(this.SetWeight));
					return;
				}
				if (emotionPreset2.PresetType == EmotionPresetType.Neutral)
				{
					emotionPreset = new EmotionPreset?(emotionPreset2);
				}
			}
			if (emotionPreset == null)
			{
				Debug.LogError(string.Format("Model {0} does not have a {1} expression set, which is a required fallback.", base.Model.name, EmotionPresetType.Neutral), base.Model);
				return;
			}
			emotionPreset.Value.SetWeights(new Action<EmotionBlendshape, float>(this.SetWeight));
		}

		private void ResetWeights()
		{
			for (int i = 0; i < this._rendCnt; i++)
			{
				this.Renderers[i].ResetWeights();
			}
		}

		private void SetWeight(EmotionBlendshape blendshape, float weight)
		{
			for (int i = 0; i < this._rendCnt; i++)
			{
				this.Renderers[i].SetWeight(blendshape, weight);
			}
		}

		public const EmotionPresetType FallbackPreset = EmotionPresetType.Neutral;

		private int _rendCnt;
	}
}
