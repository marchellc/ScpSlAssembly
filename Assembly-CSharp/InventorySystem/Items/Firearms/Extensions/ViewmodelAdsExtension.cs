using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelAdsExtension : MonoBehaviour, IViewmodelExtension
{
	[Serializable]
	private struct RandomizerSettings
	{
		public bool Randomize;

		public int TotalSights;
	}

	[Serializable]
	private class OffsetSettings
	{
		public float AdsFov;

		public Vector3 HipPosition;

		public Vector3 AdsPosition;

		public Vector3 AdsRotation;

		public Vector2 AdsSpeedMultiplierOffset;

		public float ShotAnimIntensity = 1f;

		public void Combine(OffsetSettings toCombine)
		{
			this.AdsFov += toCombine.AdsFov;
			this.HipPosition += toCombine.HipPosition;
			this.AdsPosition += toCombine.AdsPosition;
			this.AdsRotation += toCombine.AdsRotation;
			this.AdsSpeedMultiplierOffset += toCombine.AdsSpeedMultiplierOffset;
			this.ShotAnimIntensity *= toCombine.ShotAnimIntensity;
		}

		public void SetFrom(OffsetSettings source)
		{
			this.AdsFov = source.AdsFov;
			this.HipPosition = source.HipPosition;
			this.AdsPosition = source.AdsPosition;
			this.AdsRotation = source.AdsRotation;
			this.AdsSpeedMultiplierOffset = source.AdsSpeedMultiplierOffset;
			this.ShotAnimIntensity = source.ShotAnimIntensity;
		}
	}

	[Serializable]
	private class AdsAttachmentOffset
	{
		public AttachmentLink TargetAttachment;

		public OffsetSettings OffsetSettings;

		public bool OverridePrevious;
	}

	private static readonly AnimationCurve CorrectionCurve = new AnimationCurve(new Keyframe(0f, 0f, 0.052537024f, 0.052537024f, 0f, 0.13378957f), new Keyframe(0.32f, 0.5f, 2.188895f, 2.188895f, 0.3333f, 0.079453f), new Keyframe(0.65f, 1f, -0.003068474f, -0.003068474f, 0.14362735f, 0f));

	private const float OverallAdsAnimSpeedMultiplier = 5f;

	private readonly OffsetSettings _combinedSettings = new OffsetSettings();

	private AnimatedFirearmViewmodel _viewmodel;

	private Transform _viewmodelTr;

	private Firearm _firearm;

	private IAdsModule _adsModule;

	private float _animationTime;

	private bool _prevAdsTarget;

	private float _prevAdsAmount;

	private bool _initialized;

	private bool _returnAnimation;

	private bool _wasTransitioning;

	[Tooltip("Weight of AdsShotBlend at which the weapon does not move when firing.")]
	[SerializeField]
	private float _noShootingAnimBlendWeight;

	[SerializeField]
	private bool _enableShootingAnimReduction;

	[SerializeField]
	private AnimatorLayerMask _adsAnimsLayer;

	[SerializeField]
	private OffsetSettings _defaultOffset;

	[SerializeField]
	private AdsAttachmentOffset[] _attachmentOffsets;

	[SerializeField]
	private AudioClip _adsInSound;

	[SerializeField]
	private AudioClip _adsOutSound;

	[SerializeField]
	private float _adsInAnimationSpeed = 0.8f;

	[SerializeField]
	private float _adsOutAnimationSpeed = 0.8f;

	[SerializeField]
	private RandomizerSettings _randomizerSettings;

	[field: SerializeField]
	public GoopSway.GoopSwaySettings AdsSway { get; private set; }

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._firearm = viewmodel.ParentFirearm;
		this._viewmodelTr = viewmodel.transform;
		this._viewmodel = viewmodel;
		AdsAttachmentOffset[] attachmentOffsets = this._attachmentOffsets;
		for (int i = 0; i < attachmentOffsets.Length; i++)
		{
			attachmentOffsets[i].TargetAttachment.InitCache(this._firearm);
		}
		if (this._firearm.TryGetModule<IAdsModule>(out this._adsModule))
		{
			this.RefreshAttachments();
			this._initialized = true;
			this._viewmodel.OnAttachmentsUpdated += RefreshAttachments;
			if (viewmodel.IsSpectator)
			{
				this._animationTime = (this._prevAdsAmount = LinearAdsModule.GetAdsAmountForSerial(this._firearm.ItemSerial));
				this._returnAnimation = false;
			}
		}
	}

	private void RefreshAttachments()
	{
		this._combinedSettings.SetFrom(this._defaultOffset);
		for (int i = 0; i < this._attachmentOffsets.Length; i++)
		{
			AdsAttachmentOffset adsAttachmentOffset = this._attachmentOffsets[i];
			if (adsAttachmentOffset.TargetAttachment.Instance.IsEnabled)
			{
				if (adsAttachmentOffset.OverridePrevious)
				{
					this._combinedSettings.SetFrom(adsAttachmentOffset.OffsetSettings);
				}
				else
				{
					this._combinedSettings.Combine(adsAttachmentOffset.OffsetSettings);
				}
			}
		}
	}

	private void OnValidate()
	{
		if (this._initialized)
		{
			this.RefreshAttachments();
		}
	}

	private void Update()
	{
		if (this._initialized)
		{
			this.UpdateOffset();
			this.UpdateSounds();
			int[] layers = this._adsAnimsLayer.Layers;
			foreach (int layer in layers)
			{
				this.UpdateAnims(layer);
			}
		}
	}

	private void UpdateOffset()
	{
		float num = ViewmodelAdsExtension.CorrectionCurve.Evaluate(this._adsModule.AdsAmount);
		Vector3 localPosition = Vector3.Lerp(this._combinedSettings.HipPosition, this._combinedSettings.AdsPosition, num);
		Quaternion localRotation = Quaternion.Euler(this._combinedSettings.AdsRotation * num);
		this._viewmodelTr.SetLocalPositionAndRotation(localPosition, localRotation);
		this._viewmodel.FovOffset = this._combinedSettings.AdsFov * num;
	}

	private void UpdateSounds()
	{
		if (this._firearm.TryGetModule<AudioModule>(out var module))
		{
			float adsAmount = this._adsModule.AdsAmount;
			bool flag = adsAmount > 0f && adsAmount < 1f;
			if (flag && !this._wasTransitioning)
			{
				module.PlayClientside(this._adsModule.AdsTarget ? this._adsInSound : this._adsOutSound);
			}
			this._wasTransitioning = flag;
		}
	}

	private void UpdateAnims(int layer)
	{
		float adsAmount = this._adsModule.AdsAmount;
		bool adsTarget = this._adsModule.AdsTarget;
		if (this._prevAdsTarget != adsTarget)
		{
			if (adsTarget && this._prevAdsAmount <= 0f)
			{
				this._animationTime = 0f;
				this._returnAnimation = false;
				this.Randomize();
			}
			else if (!adsTarget && this._prevAdsAmount >= 1f)
			{
				this._animationTime = 1f;
				this._returnAnimation = true;
			}
		}
		float target = (adsTarget ? 1 : 0);
		float num = this._adsInAnimationSpeed + this._combinedSettings.AdsSpeedMultiplierOffset.x;
		float num2 = this._adsOutAnimationSpeed + this._combinedSettings.AdsSpeedMultiplierOffset.y;
		float num3 = (this._returnAnimation ? num2 : num);
		float num4 = this._firearm.AttachmentsValue(AttachmentParam.AdsSpeedMultiplier) * 5f * num3;
		this._animationTime = Mathf.MoveTowards(this._animationTime, target, Time.deltaTime * num4);
		if (this._returnAnimation)
		{
			this._viewmodel.AnimatorPlay(FirearmAnimatorHashes.AdsOutState, layer, 1f - this._animationTime);
			if (this._animationTime == 1f && adsTarget)
			{
				this._returnAnimation = false;
			}
		}
		else
		{
			this._viewmodel.AnimatorPlay(FirearmAnimatorHashes.AdsInState, layer, this._animationTime);
		}
		this._prevAdsTarget = adsTarget;
		this._prevAdsAmount = adsAmount;
		this._viewmodel.AnimatorSetFloat(FirearmAnimatorHashes.AdsCurrent, adsAmount);
		if (this._enableShootingAnimReduction)
		{
			float num5 = Mathf.Lerp(this._noShootingAnimBlendWeight, adsAmount, this._combinedSettings.ShotAnimIntensity);
			this._viewmodel.AnimatorSetFloat(FirearmAnimatorHashes.AdsShotBlend, adsAmount * num5);
		}
	}

	private void Randomize()
	{
		if (this._randomizerSettings.Randomize)
		{
			int num = UnityEngine.Random.Range(0, this._randomizerSettings.TotalSights);
			this._viewmodel.AnimatorSetFloat(FirearmAnimatorHashes.AdsRandom, num);
		}
	}
}
