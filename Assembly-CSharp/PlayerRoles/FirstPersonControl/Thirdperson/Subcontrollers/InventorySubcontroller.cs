using System;
using System.Collections.Generic;
using AnimatorLayerManagement;
using GameObjectPools;
using InventorySystem;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class InventorySubcontroller : SubcontrollerBehaviour, IPoolResettable, ILookatModifier, IHandPoseModifier
{
	[Serializable]
	private struct DefaultAnimatorOverrides
	{
		public AnimationClip Original;

		public AnimationClip Override;
	}

	private enum TransitionStatus
	{
		RetractingPrevious,
		EquippingNew,
		Done
	}

	public static Action<ThirdpersonItemBase> OnSwapThirdpersonItem;

	private const float DefaultTransitionTime = 0.5f;

	private ItemIdentifier _prevItem;

	private Dictionary<AnimationClip, AnimationClip> _dictionarizedDefaultOverrides;

	private float _weightAdjustSpeed;

	private TransitionStatus _transitionStatus;

	private bool _hasThirdpersonInstance;

	private ThirdpersonItemBase _lastThirdpersonInstance;

	private int _regularIdleLayerIndex;

	private int _itemIdleLayerIndex;

	private int? _cuffedOverrideLayerIndex;

	private int _movementOverrideLayerIndex;

	private float _transitionWeight;

	private float _prevCuffWeight;

	[SerializeField]
	private ItemLayerLink[] _itemLayers;

	[SerializeField]
	private DefaultAnimatorOverrides[] _defaultAnimatorOverrides;

	[SerializeField]
	private LayerRefId _regularIdleLayer;

	[SerializeField]
	private LayerRefId _itemIdleLayer;

	[SerializeField]
	private LayerRefId _movementOverrideLayer;

	[SerializeField]
	private LayerRefId _cuffedOverrideLayer;

	[SerializeField]
	private float _cuffedAdjustSpeed;

	public bool PoolHeldItem;

	public Transform ItemSpawnpoint { get; private set; }

	public float TransitionWeight
	{
		get
		{
			return this._transitionWeight;
		}
		private set
		{
			this._transitionWeight = Mathf.Clamp01(value);
		}
	}

	public event Action OnHeldItemUpdated;

	private void OnAnimatorIK(int layerIndex)
	{
		if (!base.Culled && this.TryGetCurrentInstance(out var instance))
		{
			instance.OnAnimIK(layerIndex, this.TransitionWeight);
		}
	}

	private void OnFadeChanged()
	{
		if (this.TryGetCurrentInstance(out var instance))
		{
			instance.OnFadeChanged(base.Model.Fade);
		}
	}

	private void OnVisibilityChanged()
	{
	}

	private void ResetThirdpersonItem()
	{
		if (this._dictionarizedDefaultOverrides == null)
		{
			this._dictionarizedDefaultOverrides = new Dictionary<AnimationClip, AnimationClip>(this._defaultAnimatorOverrides.Length);
			DefaultAnimatorOverrides[] defaultAnimatorOverrides = this._defaultAnimatorOverrides;
			for (int i = 0; i < defaultAnimatorOverrides.Length; i++)
			{
				DefaultAnimatorOverrides defaultAnimatorOverrides2 = defaultAnimatorOverrides[i];
				this._dictionarizedDefaultOverrides.Add(defaultAnimatorOverrides2.Original, defaultAnimatorOverrides2.Override);
			}
		}
		ThirdpersonItemAnimationManager.ResetOverrides(base.Model, this._dictionarizedDefaultOverrides);
	}

	private void SwapThirdpersonItem(ItemIdentifier itemId)
	{
		this.ResetThirdpersonItem();
		if (this.TryGetCurrentInstance(out var instance))
		{
			instance.ReturnToPool();
			this._hasThirdpersonInstance = false;
		}
		this._transitionStatus = TransitionStatus.EquippingNew;
		this._hasThirdpersonInstance = ThirdpersonItemPoolManager.TryGet(this, itemId, out this._lastThirdpersonInstance, this.PoolHeldItem);
		InventorySubcontroller.OnSwapThirdpersonItem?.Invoke(this._lastThirdpersonInstance);
	}

	private float GetTransitionTime(ItemIdentifier itemId)
	{
		if (itemId.TypeId.TryGetTemplate<ItemBase>(out var item))
		{
			ThirdpersonItemBase thirdpersonModel = item.ThirdpersonModel;
			if (thirdpersonModel != null)
			{
				return Mathf.Max(thirdpersonModel.GetTransitionTime(itemId), Time.deltaTime);
			}
		}
		return 0.5f;
	}

	private void UpdateLayerWeights()
	{
		float num = Mathf.SmoothStep(0f, 1f, this.TransitionWeight);
		float num2 = (this._hasThirdpersonInstance ? num : 0f);
		float walkLayerWeight = base.Model.WalkLayerWeight;
		ItemLayerLink[] itemLayers = this._itemLayers;
		foreach (ItemLayerLink itemLayerLink in itemLayers)
		{
			float num3 = num;
			if (this.TryGetCurrentInstance(out var instance))
			{
				num3 *= instance.GetWeightForLayer(itemLayerLink.Layer3p).Weight;
			}
			int layerIndex = itemLayerLink.GetLayerIndex(base.Model);
			base.Animator.SetLayerWeight(layerIndex, num3);
		}
		base.Animator.SetLayerWeight(this._movementOverrideLayerIndex, walkLayerWeight * num2);
		base.Animator.SetLayerWeight(this._itemIdleLayerIndex, num2);
		base.Animator.SetLayerWeight(this._regularIdleLayerIndex, 1f - num2);
	}

	private void UpdateCuffWeight()
	{
		if (this._cuffedOverrideLayerIndex.HasValue)
		{
			float target = (base.OwnerHub.inventory.IsDisarmed() ? 1 : 0);
			float maxDelta = Time.deltaTime * this._cuffedAdjustSpeed;
			float num = Mathf.MoveTowards(this._prevCuffWeight, target, maxDelta);
			if (num != this._prevCuffWeight)
			{
				base.Animator.SetLayerWeight(this._cuffedOverrideLayerIndex.Value, num);
				this._prevCuffWeight = num;
			}
		}
	}

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		Transform boneTransform = model.Animator.GetBoneTransform(HumanBodyBones.RightHand);
		this.ItemSpawnpoint = new GameObject().transform;
		this.ItemSpawnpoint.SetParent(boneTransform, worldPositionStays: true);
		this.ItemSpawnpoint.ResetLocalPose();
		this._weightAdjustSpeed = 1f;
		model.OnFadeChanged += OnFadeChanged;
		model.OnVisibilityChanged += OnVisibilityChanged;
		this.CacheLayerIndexes();
	}

	private void CacheLayerIndexes()
	{
		AnimatorLayerManager layerManager = base.Model.LayerManager;
		this._itemIdleLayerIndex = layerManager.GetLayerIndex(this._itemIdleLayer);
		this._regularIdleLayerIndex = layerManager.GetLayerIndex(this._regularIdleLayer);
		this._movementOverrideLayerIndex = layerManager.GetLayerIndex(this._movementOverrideLayer);
		this._cuffedOverrideLayerIndex = ((this._cuffedAdjustSpeed > 0f) ? new int?(layerManager.GetLayerIndex(this._cuffedOverrideLayer)) : ((int?)null));
	}

	private void Update()
	{
		if (!base.Model.Pooled && !base.OwnerHub.isLocalPlayer)
		{
			this.UpdateHeldItem(base.OwnerHub.inventory.CurItem);
			this.UpdateCuffWeight();
		}
	}

	public bool TryGetCurrentInstance(out ThirdpersonItemBase instance)
	{
		instance = this._lastThirdpersonInstance;
		return this._hasThirdpersonInstance;
	}

	public bool TryGetCurrentInstance<T>(out T instance)
	{
		if (this.TryGetCurrentInstance(out var instance2) && instance2 is T val)
		{
			instance = val;
			return true;
		}
		instance = default(T);
		return false;
	}

	public void ResetObject()
	{
		this._prevItem = ItemIdentifier.None;
		this._prevCuffWeight = 0f;
		this.ResetThirdpersonItem();
		if (this.TryGetCurrentInstance(out var instance))
		{
			instance.ReturnToPool();
			this._hasThirdpersonInstance = false;
		}
	}

	public void UpdateHeldItem(ItemIdentifier itemId, bool instant = false)
	{
		if (this._prevItem != itemId)
		{
			this._prevItem = itemId;
			this._transitionStatus = TransitionStatus.RetractingPrevious;
			this._weightAdjustSpeed = 2f / this.GetTransitionTime(itemId);
			if (instant)
			{
				this.SwapThirdpersonItem(itemId);
				this.TransitionWeight = 1f;
				return;
			}
		}
		switch (this._transitionStatus)
		{
		case TransitionStatus.RetractingPrevious:
			this.TransitionWeight -= Time.deltaTime * this._weightAdjustSpeed;
			if (this.TransitionWeight <= 0f)
			{
				this.SwapThirdpersonItem(itemId);
			}
			break;
		case TransitionStatus.EquippingNew:
			this.TransitionWeight += Time.deltaTime * this._weightAdjustSpeed;
			if (this.TransitionWeight >= 1f)
			{
				this._transitionStatus = TransitionStatus.Done;
			}
			break;
		}
		this.UpdateLayerWeights();
		this.OnHeldItemUpdated?.Invoke();
	}

	public LookatData ProcessLookat(LookatData original)
	{
		if (!this.TryGetCurrentInstance(out ILookatModifier instance))
		{
			return original;
		}
		LookatData target = instance.ProcessLookat(original);
		return original.LerpTo(target, this.TransitionWeight);
	}

	public HandPoseData ProcessHandPose(HandPoseData original)
	{
		if (!this.TryGetCurrentInstance(out IHandPoseModifier instance))
		{
			return original;
		}
		HandPoseData target = instance.ProcessHandPose(original);
		return original.LerpTo(target, this.TransitionWeight);
	}
}
