using System;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

[PresetPrefabExtension("Holo Sight Renderer", false)]
public class ViewmodelReflexSightExtension : MonoBehaviour, IViewmodelExtension
{
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

	private void LateUpdate()
	{
		if (this._firearm.TryGetModule<IAdsModule>(out var module))
		{
			float adsAmount = module.AdsAmount;
			if (!this._prevAds.HasValue || this._prevAds.Value != adsAmount)
			{
				ViewmodelReflexSightExtension._materialInstance.SetColor(ViewmodelReflexSightExtension.HashColor, this._targetColor * adsAmount);
				this._prevAds = adsAmount;
			}
		}
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
		int relativesCount = 0;
		GameObject gameObject = base.gameObject;
		do
		{
			ViewmodelReflexSightExtension.RelativesNonAlloc[relativesCount++] = gameObject;
			gameObject = gameObject.transform.parent.gameObject;
		}
		while (!(gameObject == null) && relativesCount != ViewmodelReflexSightExtension.RelativesNonAlloc.Length);
		for (int i = 0; i < viewmodel.Attachments.Length; i++)
		{
			GameObject[] array = viewmodel.Attachments[i].Group;
			for (int j = 0; j < array.Length; j++)
			{
				if (IsRelated(array[j]))
				{
					this._sightAtt = this._firearm.Attachments[i] as ReflexSightAttachment;
					return;
				}
			}
		}
		Debug.LogError("No reflex sight attachment found for '" + base.name + "'!");
		bool IsRelated(GameObject other)
		{
			for (int k = 0; k < relativesCount; k++)
			{
				if (other == ViewmodelReflexSightExtension.RelativesNonAlloc[k])
				{
					return true;
				}
			}
			return false;
		}
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
		sightAtt.OnValuesChanged = (Action)Delegate.Combine(sightAtt.OnValuesChanged, new Action(UpdateValues));
		this._initialized = true;
	}
}
