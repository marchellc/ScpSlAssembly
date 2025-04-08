using System;
using System.Runtime.CompilerServices;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	[PresetPrefabExtension("Holo Sight Renderer", false)]
	public class ViewmodelReflexSightExtension : MonoBehaviour, IViewmodelExtension
	{
		private void LateUpdate()
		{
			IAdsModule adsModule;
			if (!this._firearm.TryGetModule(out adsModule, true))
			{
				return;
			}
			float adsAmount = adsModule.AdsAmount;
			if (this._prevAds != null && this._prevAds.Value == adsAmount)
			{
				return;
			}
			ViewmodelReflexSightExtension._materialInstance.SetColor(ViewmodelReflexSightExtension.HashColor, this._targetColor * adsAmount);
			this._prevAds = new float?(adsAmount);
		}

		private void OnEnable()
		{
			if (this._initialized)
			{
				this.UpdateValues();
			}
		}

		private void UpdateValues()
		{
			this.SetMaterial(this._sightAtt.TextureOptions[this._sightAtt.CurTextureIndex], ReflexSightAttachment.Sizes[this._sightAtt.CurSizeIndex], ReflexSightAttachment.Colors[this._sightAtt.CurColorIndex], ReflexSightAttachment.BrightnessLevels[this._sightAtt.CurBrightnessIndex]);
		}

		private void SetMaterial(Texture texture, float size, Color color, float brigthness)
		{
			ViewmodelReflexSightExtension._materialInstance.SetTexture(ViewmodelReflexSightExtension.HashTexture, texture);
			ViewmodelReflexSightExtension._materialInstance.SetFloat(ViewmodelReflexSightExtension.HashSize, size);
			this._targetColor = Color.Lerp(color, Color.white, brigthness);
			this._prevAds = null;
		}

		private void FindSightAttachment(AnimatedFirearmViewmodel viewmodel)
		{
			ViewmodelReflexSightExtension.<>c__DisplayClass15_0 CS$<>8__locals1;
			CS$<>8__locals1.relativesCount = 0;
			GameObject gameObject = base.gameObject;
			do
			{
				GameObject[] relativesNonAlloc = ViewmodelReflexSightExtension.RelativesNonAlloc;
				int i = CS$<>8__locals1.relativesCount;
				CS$<>8__locals1.relativesCount = i + 1;
				relativesNonAlloc[i] = gameObject;
				gameObject = gameObject.transform.parent.gameObject;
			}
			while (!(gameObject == null) && CS$<>8__locals1.relativesCount != ViewmodelReflexSightExtension.RelativesNonAlloc.Length);
			for (int j = 0; j < viewmodel.Attachments.Length; j++)
			{
				GameObject[] group = viewmodel.Attachments[j].Group;
				for (int i = 0; i < group.Length; i++)
				{
					if (ViewmodelReflexSightExtension.<FindSightAttachment>g__IsRelated|15_0(group[i], ref CS$<>8__locals1))
					{
						this._sightAtt = this._firearm.Attachments[j] as ReflexSightAttachment;
						return;
					}
				}
			}
			Debug.LogError("No reflex sight attachment found for '" + base.name + "'!");
		}

		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._firearm = viewmodel.ParentFirearm;
			if (ViewmodelReflexSightExtension._materialInstance == null)
			{
				ViewmodelReflexSightExtension._materialInstance = new Material(this._targetRenderer.material);
			}
			this._targetRenderer.sharedMaterial = ViewmodelReflexSightExtension._materialInstance;
			this.FindSightAttachment(viewmodel);
			this.UpdateValues();
			ReflexSightAttachment sightAtt = this._sightAtt;
			sightAtt.OnValuesChanged = (Action)Delegate.Combine(sightAtt.OnValuesChanged, new Action(this.UpdateValues));
			this._initialized = true;
		}

		[CompilerGenerated]
		internal static bool <FindSightAttachment>g__IsRelated|15_0(GameObject other, ref ViewmodelReflexSightExtension.<>c__DisplayClass15_0 A_1)
		{
			for (int i = 0; i < A_1.relativesCount; i++)
			{
				if (other == ViewmodelReflexSightExtension.RelativesNonAlloc[i])
				{
					return true;
				}
			}
			return false;
		}

		private static readonly int HashTexture = Shader.PropertyToID("_CrosshairTex");

		private static readonly int HashColor = Shader.PropertyToID("_CrosshairColor");

		private static readonly int HashSize = Shader.PropertyToID("_SizeMultiplier");

		private static readonly GameObject[] RelativesNonAlloc = new GameObject[4];

		private static Material _materialInstance;

		private Firearm _firearm;

		private ReflexSightAttachment _sightAtt;

		private Color _targetColor;

		private float? _prevAds;

		private bool _initialized;

		[SerializeField]
		private Renderer _targetRenderer;
	}
}
