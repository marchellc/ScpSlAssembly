using System.Collections.Generic;
using System.Diagnostics;
using AnimatorLayerManagement;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Thirdperson;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Thirdperson;

public class FirearmThirdpersonItem : ThirdpersonItemBase, ILookatModifier, IHandPoseModifier
{
	private enum FirearmAnim
	{
		Ads,
		Hip,
		Reload
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

	public bool WorldmodelInstanceSet { get; private set; }

	public FirearmWorldmodel WorldmodelInstance { get; private set; }

	public override float GetTransitionTime(ItemIdentifier iid)
	{
		if (!InventoryItemLoader.TryGetItem<Firearm>(iid.TypeId, out var result))
		{
			return 0.5f;
		}
		if (!result.TryGetModule<IEquipperModule>(out var module))
		{
			return 0.5f;
		}
		return Mathf.Min(module.DisplayBaseEquipTime, 0.5f);
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
			float a = Mathf.Clamp01(1f - this._lastReloadBlend);
			a = Mathf.Lerp(a, 1f, Mathf.Abs(data.LookDir.y / 2f));
			data.GlobalWeight *= a;
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
		if (!base.Pooled)
		{
			this.UpdateAnims();
		}
	}

	internal override void OnFadeChanged(float newFade)
	{
		base.OnFadeChanged(newFade);
		foreach (KeyValuePair<Light, Color> defaultColor in this._defaultColors)
		{
			defaultColor.Key.color = Color.Lerp(Color.black, defaultColor.Value, newFade);
		}
	}

	internal override void Initialize(InventorySubcontroller srctr, ItemIdentifier id)
	{
		if (!this.WorldmodelInstanceSet && InventoryItemLoader.TryGetItem<Firearm>(id.TypeId, out this._template))
		{
			this.WorldmodelInstance = Object.Instantiate(this._template.WorldModel, base.transform);
			this.WorldmodelInstance.transform.SetLocalPositionAndRotation(this._localPosition, Quaternion.Euler(this._localRotation));
			this.WorldmodelInstanceSet = true;
		}
		if (!this._eventAssigned)
		{
			AttachmentCodeSync.OnReceived += OnAttachmentsUpdated;
			this._eventAssigned = true;
		}
		base.Initialize(srctr, id);
		this.WorldmodelInstance.Setup(base.ItemId, srctr.Model.HasOwner ? FirearmWorldmodelType.Thirdperson : FirearmWorldmodelType.Presentation);
		base.SetAnim(AnimState3p.Override1, this._hipAnim);
		base.SetAnim(AnimState3p.Override0, this._adsAnim);
		base.SetAnim(AnimState3p.Override2, this._reloadAnim);
		this._additionalOverrideClips.ForEach(base.SetAnim);
		this._rightHandIK.Initialize(this.WorldmodelInstance, base.TargetModel);
		this._leftHandIK.Initialize(this.WorldmodelInstance, base.TargetModel);
		base.OverrideBlend = 1f;
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
		if (this.WorldmodelInstanceSet && serial == base.ItemId.SerialNumber)
		{
			this.WorldmodelInstance.Setup(base.ItemId, FirearmWorldmodelType.Thirdperson, code);
		}
	}

	private void Awake()
	{
		Light[] componentsInChildren = base.GetComponentsInChildren<Light>(includeInactive: true);
		foreach (Light light in componentsInChildren)
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
		if (this._eventAssigned)
		{
			AttachmentCodeSync.OnReceived -= OnAttachmentsUpdated;
			this._eventAssigned = false;
		}
	}

	private void UpdateAnims()
	{
		FirearmAnim firearmAnim = ((this._isReloading && this._reloadElapsed.Elapsed.TotalSeconds < (double)this._maxReloadTimeSeconds) ? FirearmAnim.Reload : ((!this._isAdsing) ? FirearmAnim.Hip : FirearmAnim.Ads));
		base.OverrideBlend = Mathf.MoveTowards(base.OverrideBlend, (float)firearmAnim, Time.deltaTime * 4f);
		this._prevAdsBlend = Mathf.Clamp01(1f - base.OverrideBlend);
		this._lastReloadBlend = base.OverrideBlend - 2f + 1f;
		if (this._shotReceived)
		{
			this._shotReceived = false;
		}
		if (this._template.TryGetModule<IAdsModule>(out var module))
		{
			module.GetDisplayAdsValues(base.ItemId.SerialNumber, out this._isAdsing, out var _);
		}
		if (this._template.TryGetModule<IReloaderModule>(out var module2))
		{
			bool isReloading = this._isReloading;
			this._isReloading = module2.GetDisplayReloadingOrUnloading(base.ItemId.SerialNumber);
			if (this._isReloading && !isReloading)
			{
				this._reloadElapsed.Restart();
				base.ReplayOverrideBlend(soft: true);
			}
		}
	}
}
