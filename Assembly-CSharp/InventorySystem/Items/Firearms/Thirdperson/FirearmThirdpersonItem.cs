using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnimatorLayerManagement;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Thirdperson;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Thirdperson
{
	public class FirearmThirdpersonItem : ThirdpersonItemBase, ILookatModifier, IHandPoseModifier
	{
		public bool WorldmodelInstanceSet { get; private set; }

		public FirearmWorldmodel WorldmodelInstance { get; private set; }

		public override float GetTransitionTime(ItemIdentifier iid)
		{
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(iid.TypeId, out firearm))
			{
				return 0.5f;
			}
			IEquipperModule equipperModule;
			if (!firearm.TryGetModule(out equipperModule, true))
			{
				return 0.5f;
			}
			return Mathf.Min(equipperModule.DisplayBaseEquipTime, 0.5f);
		}

		public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			ThirdpersonLayerWeight weightForLayer = this._regularLayerProcessor.GetWeightForLayer(this, layer);
			ThirdpersonLayerWeight weightForLayer2 = this._reloadLayerProcessor.GetWeightForLayer(this, layer);
			return ThirdpersonLayerWeight.Lerp(weightForLayer, weightForLayer2, this._lastReloadBlend);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._isReloading = false;
			this._isAdsing = false;
			this.UnsubscribeEvents();
		}

		public LookatData ProcessLookat(LookatData data)
		{
			data.BodyWeight = Mathf.Clamp01(data.BodyWeight * this._bodyIkMultiplier + this._bodyIkAbsolute);
			if (this._lastReloadBlend > 0f)
			{
				float num = Mathf.Clamp01(1f - this._lastReloadBlend);
				num = Mathf.Lerp(num, 1f, Mathf.Abs(data.LookDir.y / 2f));
				data.GlobalWeight *= num;
			}
			return data;
		}

		public HandPoseData ProcessHandPose(HandPoseData data)
		{
			data = this._leftHandIK.ProcessHandPose(data);
			data = this._rightHandIK.ProcessHandPose(data);
			return data;
		}

		protected override void Update()
		{
			base.Update();
			if (base.Pooled)
			{
				return;
			}
			this.UpdateAnims();
		}

		internal override void OnFadeChanged(float newFade)
		{
			base.OnFadeChanged(newFade);
			foreach (KeyValuePair<Light, Color> keyValuePair in this._defaultColors)
			{
				keyValuePair.Key.color = Color.Lerp(Color.black, keyValuePair.Value, newFade);
			}
		}

		internal override void Initialize(InventorySubcontroller srctr, ItemIdentifier id)
		{
			if (!this.WorldmodelInstanceSet && InventoryItemLoader.TryGetItem<Firearm>(id.TypeId, out this._template))
			{
				this.WorldmodelInstance = global::UnityEngine.Object.Instantiate<FirearmWorldmodel>(this._template.WorldModel, base.transform);
				this.WorldmodelInstance.transform.SetLocalPositionAndRotation(this._localPosition, Quaternion.Euler(this._localRotation));
				this.WorldmodelInstanceSet = true;
			}
			if (!this._eventAssigned)
			{
				AttachmentCodeSync.OnReceived += this.OnAttachmentsUpdated;
				this._eventAssigned = true;
			}
			base.Initialize(srctr, id);
			this.WorldmodelInstance.Setup(base.ItemId, FirearmWorldmodelType.Thirdperson);
			base.SetAnim(AnimState3p.Override1, this._hipAnim);
			base.SetAnim(AnimState3p.Override0, this._adsAnim);
			base.SetAnim(AnimState3p.Override2, this._reloadAnim);
			this._additionalOverrideClips.ForEach(new Action<AnimOverrideState3pPair>(base.SetAnim));
			this._rightHandIK.Initialize(this.WorldmodelInstance, base.TargetModel);
			this._leftHandIK.Initialize(this.WorldmodelInstance, base.TargetModel);
		}

		internal override void OnAnimIK(int layerIndex, float ikScale)
		{
			base.OnAnimIK(layerIndex, ikScale);
			ikScale = Mathf.Clamp01(ikScale - this._lastReloadBlend);
			AnimatorLayerManager layerManager = base.TargetModel.LayerManager;
			if (layerIndex == layerManager.GetLayerIndex(this._ikLayerRightHand))
			{
				this._rightHandIK.IKUpdateRightHandRotation(ikScale, this._prevAdsBlend);
			}
			if (layerIndex == layerManager.GetLayerIndex(this._ikLayerLeftHand))
			{
				this._leftHandIK.IKUpdateLeftHandAnchor(ikScale);
			}
		}

		private void OnAttachmentsUpdated(ushort serial, uint code)
		{
			if (!this.WorldmodelInstanceSet)
			{
				return;
			}
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.WorldmodelInstance.Setup(base.ItemId, FirearmWorldmodelType.Thirdperson, code);
		}

		private void Awake()
		{
			foreach (Light light in base.GetComponentsInChildren<Light>(true))
			{
				this._defaultColors[light] = light.color;
			}
		}

		private void OnDestroy()
		{
			this.UnsubscribeEvents();
		}

		private void UnsubscribeEvents()
		{
			if (!this._eventAssigned)
			{
				return;
			}
			AttachmentCodeSync.OnReceived -= this.OnAttachmentsUpdated;
			this._eventAssigned = false;
		}

		private void UpdateAnims()
		{
			FirearmThirdpersonItem.FirearmAnim firearmAnim = ((this._isReloading && this._reloadElapsed.Elapsed.TotalSeconds < (double)this._maxReloadTimeSeconds) ? FirearmThirdpersonItem.FirearmAnim.Reload : (this._isAdsing ? FirearmThirdpersonItem.FirearmAnim.Ads : FirearmThirdpersonItem.FirearmAnim.Hip));
			base.OverrideBlend = Mathf.MoveTowards(base.OverrideBlend, (float)firearmAnim, Time.deltaTime * 4f);
			this._prevAdsBlend = Mathf.Clamp01(1f - base.OverrideBlend);
			this._lastReloadBlend = base.OverrideBlend - 2f + 1f;
			if (this._shotReceived)
			{
				this._shotReceived = false;
			}
			IAdsModule adsModule;
			if (this._template.TryGetModule(out adsModule, true))
			{
				float num;
				adsModule.GetDisplayAdsValues(base.ItemId.SerialNumber, out this._isAdsing, out num);
			}
			IReloaderModule reloaderModule;
			if (this._template.TryGetModule(out reloaderModule, true))
			{
				bool isReloading = this._isReloading;
				this._isReloading = reloaderModule.GetDisplayReloadingOrUnloading(base.ItemId.SerialNumber);
				if (this._isReloading && !isReloading)
				{
					this._reloadElapsed.Restart();
					base.ReplayOverrideBlend(true);
				}
			}
		}

		private readonly Dictionary<Light, Color> _defaultColors = new Dictionary<Light, Color>();

		private readonly Stopwatch _reloadElapsed = Stopwatch.StartNew();

		private const float DefaultEquipTime = 0.5f;

		private const float BlendTransitionSpeed = 4f;

		[SerializeField]
		private AnimationClip _hipAnim;

		[SerializeField]
		private AnimationClip _adsAnim;

		[SerializeField]
		private AnimationClip _reloadAnim;

		[SerializeField]
		private AnimOverrideState3pPair[] _additionalOverrideClips;

		[SerializeField]
		private bool _isAdsing;

		[SerializeField]
		private bool _isReloading;

		[SerializeField]
		private float _maxReloadTimeSeconds;

		[SerializeField]
		private LayerProcessorBase _regularLayerProcessor;

		[SerializeField]
		private LayerProcessorBase _reloadLayerProcessor;

		[Header("Inverse Kinematics")]
		[SerializeField]
		private LayerRefId _ikLayerRightHand;

		[SerializeField]
		private LayerRefId _ikLayerLeftHand;

		[SerializeField]
		private float _bodyIkMultiplier;

		[SerializeField]
		private float _bodyIkAbsolute;

		[SerializeField]
		private Vector3 _localPosition;

		[SerializeField]
		private Vector3 _localRotation;

		[SerializeField]
		private LeftHandIKHandler _leftHandIK;

		[SerializeField]
		private RightHandIKHandler _rightHandIK;

		[SerializeField]
		private bool _editorPauseIK;

		private float _prevAdsBlend;

		private bool _shotReceived;

		private bool _eventAssigned;

		private float _lastReloadBlend;

		private Firearm _template;

		private enum FirearmAnim
		{
			Ads,
			Hip,
			Reload
		}
	}
}
