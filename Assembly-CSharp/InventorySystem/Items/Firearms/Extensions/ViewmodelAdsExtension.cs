using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class ViewmodelAdsExtension : MonoBehaviour, IViewmodelExtension
	{
		public GoopSway.GoopSwaySettings AdsSway { get; private set; }

		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._firearm = viewmodel.ParentFirearm;
			this._viewmodelTr = viewmodel.transform;
			this._viewmodel = viewmodel;
			ViewmodelAdsExtension.AdsAttachmentOffset[] attachmentOffsets = this._attachmentOffsets;
			for (int i = 0; i < attachmentOffsets.Length; i++)
			{
				attachmentOffsets[i].TargetAttachment.InitCache(this._firearm);
			}
			if (!this._firearm.TryGetModule(out this._adsModule, true))
			{
				return;
			}
			this.RefreshAttachments();
			this._initialized = true;
			this._viewmodel.OnAttachmentsUpdated += this.RefreshAttachments;
			if (!viewmodel.IsSpectator)
			{
				return;
			}
			float adsAmountForSerial = LinearAdsModule.GetAdsAmountForSerial(this._firearm.ItemSerial);
			this._prevAdsAmount = adsAmountForSerial;
			this._animationTime = adsAmountForSerial;
			this._returnAnimation = false;
		}

		private void RefreshAttachments()
		{
			this._combinedSettings.SetFrom(this._defaultOffset);
			for (int i = 0; i < this._attachmentOffsets.Length; i++)
			{
				ViewmodelAdsExtension.AdsAttachmentOffset adsAttachmentOffset = this._attachmentOffsets[i];
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
			if (!this._initialized)
			{
				return;
			}
			this.RefreshAttachments();
		}

		private void Update()
		{
			if (!this._initialized)
			{
				return;
			}
			this.UpdateOffset();
			this.UpdateSounds();
			foreach (int num in this._adsAnimsLayer.Layers)
			{
				this.UpdateAnims(num);
			}
		}

		private void UpdateOffset()
		{
			float num = ViewmodelAdsExtension.CorrectionCurve.Evaluate(this._adsModule.AdsAmount);
			Vector3 vector = Vector3.Lerp(this._combinedSettings.HipPosition, this._combinedSettings.AdsPosition, num);
			Quaternion quaternion = Quaternion.Euler(this._combinedSettings.AdsRotation * num);
			this._viewmodelTr.SetLocalPositionAndRotation(vector, quaternion);
			this._viewmodel.FovOffset = this._combinedSettings.AdsFov * num;
		}

		private void UpdateSounds()
		{
			AudioModule audioModule;
			if (!this._firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			float adsAmount = this._adsModule.AdsAmount;
			bool flag = adsAmount > 0f && adsAmount < 1f;
			if (flag && !this._wasTransitioning)
			{
				audioModule.PlayClientside(this._adsModule.AdsTarget ? this._adsInSound : this._adsOutSound);
			}
			this._wasTransitioning = flag;
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
			float num = (float)(adsTarget ? 1 : 0);
			float num2 = this._adsInAnimationSpeed + this._combinedSettings.AdsSpeedMultiplierOffset.x;
			float num3 = this._adsOutAnimationSpeed + this._combinedSettings.AdsSpeedMultiplierOffset.y;
			float num4 = (this._returnAnimation ? num3 : num2);
			float num5 = this._firearm.AttachmentsValue(AttachmentParam.AdsSpeedMultiplier) * 5f * num4;
			this._animationTime = Mathf.MoveTowards(this._animationTime, num, Time.deltaTime * num5);
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
			if (!this._enableShootingAnimReduction)
			{
				return;
			}
			float num6 = Mathf.Lerp(this._noShootingAnimBlendWeight, adsAmount, this._combinedSettings.ShotAnimIntensity);
			this._viewmodel.AnimatorSetFloat(FirearmAnimatorHashes.AdsShotBlend, adsAmount * num6);
		}

		private void Randomize()
		{
			if (!this._randomizerSettings.Randomize)
			{
				return;
			}
			int num = global::UnityEngine.Random.Range(0, this._randomizerSettings.TotalSights);
			this._viewmodel.AnimatorSetFloat(FirearmAnimatorHashes.AdsRandom, (float)num);
		}

		private static readonly AnimationCurve CorrectionCurve = new AnimationCurve(new Keyframe[]
		{
			new Keyframe(0f, 0f, 0.052537024f, 0.052537024f, 0f, 0.13378957f),
			new Keyframe(0.32f, 0.5f, 2.188895f, 2.188895f, 0.3333f, 0.079453f),
			new Keyframe(0.65f, 1f, -0.003068474f, -0.003068474f, 0.14362735f, 0f)
		});

		private const float OverallAdsAnimSpeedMultiplier = 5f;

		private readonly ViewmodelAdsExtension.OffsetSettings _combinedSettings = new ViewmodelAdsExtension.OffsetSettings();

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
		private ViewmodelAdsExtension.OffsetSettings _defaultOffset;

		[SerializeField]
		private ViewmodelAdsExtension.AdsAttachmentOffset[] _attachmentOffsets;

		[SerializeField]
		private AudioClip _adsInSound;

		[SerializeField]
		private AudioClip _adsOutSound;

		[SerializeField]
		private float _adsInAnimationSpeed = 0.8f;

		[SerializeField]
		private float _adsOutAnimationSpeed = 0.8f;

		[SerializeField]
		private ViewmodelAdsExtension.RandomizerSettings _randomizerSettings;

		[Serializable]
		private struct RandomizerSettings
		{
			public bool Randomize;

			public int TotalSights;
		}

		[Serializable]
		private class OffsetSettings
		{
			public void Combine(ViewmodelAdsExtension.OffsetSettings toCombine)
			{
				this.AdsFov += toCombine.AdsFov;
				this.HipPosition += toCombine.HipPosition;
				this.AdsPosition += toCombine.AdsPosition;
				this.AdsRotation += toCombine.AdsRotation;
				this.AdsSpeedMultiplierOffset += toCombine.AdsSpeedMultiplierOffset;
				this.ShotAnimIntensity *= toCombine.ShotAnimIntensity;
			}

			public void SetFrom(ViewmodelAdsExtension.OffsetSettings source)
			{
				this.AdsFov = source.AdsFov;
				this.HipPosition = source.HipPosition;
				this.AdsPosition = source.AdsPosition;
				this.AdsRotation = source.AdsRotation;
				this.AdsSpeedMultiplierOffset = source.AdsSpeedMultiplierOffset;
				this.ShotAnimIntensity = source.ShotAnimIntensity;
			}

			public float AdsFov;

			public Vector3 HipPosition;

			public Vector3 AdsPosition;

			public Vector3 AdsRotation;

			public Vector2 AdsSpeedMultiplierOffset;

			public float ShotAnimIntensity = 1f;
		}

		[Serializable]
		private class AdsAttachmentOffset
		{
			public AttachmentLink TargetAttachment;

			public ViewmodelAdsExtension.OffsetSettings OffsetSettings;

			public bool OverridePrevious;
		}
	}
}
