using System;
using System.Collections.Generic;
using AnimatorLayerManagement;
using GameObjectPools;
using InventorySystem;
using InventorySystem.Disarming;
using InventorySystem.Items;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class InventorySubcontroller : SubcontrollerBehaviour, IPoolResettable, ILookatModifier, IHandPoseModifier
	{
		public event Action OnUpdated;

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

		private void OnAnimatorIK(int layerIndex)
		{
			ThirdpersonItemBase thirdpersonItemBase;
			if (base.Culled || !this.TryGetCurrentInstance(out thirdpersonItemBase))
			{
				return;
			}
			thirdpersonItemBase.OnAnimIK(layerIndex, this.TransitionWeight);
		}

		private void OnFadeChanged()
		{
			ThirdpersonItemBase thirdpersonItemBase;
			if (!this.TryGetCurrentInstance(out thirdpersonItemBase))
			{
				return;
			}
			thirdpersonItemBase.OnFadeChanged(base.Model.Fade);
		}

		private void OnVisibilityChanged()
		{
		}

		private void ResetThirdpersonItem()
		{
			if (this._dictionarizedDefaultOverrides == null)
			{
				this._dictionarizedDefaultOverrides = new Dictionary<AnimationClip, AnimationClip>(this._defaultAnimatorOverrides.Length);
				foreach (InventorySubcontroller.DefaultAnimatorOverrides defaultAnimatorOverrides2 in this._defaultAnimatorOverrides)
				{
					this._dictionarizedDefaultOverrides.Add(defaultAnimatorOverrides2.Original, defaultAnimatorOverrides2.Override);
				}
			}
			ThirdpersonItemAnimationManager.ResetOverrides(base.Model, this._dictionarizedDefaultOverrides);
		}

		private void SwapThirdpersonItem(ItemIdentifier itemId)
		{
			this.ResetThirdpersonItem();
			ThirdpersonItemBase thirdpersonItemBase;
			if (this.TryGetCurrentInstance(out thirdpersonItemBase))
			{
				thirdpersonItemBase.ReturnToPool(true);
				this._hasThirdpersonInstance = false;
			}
			this._transitionStatus = InventorySubcontroller.TransitionStatus.EquippingNew;
			this._hasThirdpersonInstance = ThirdpersonItemPoolManager.TryGet(this, itemId, out this._lastThirdpersonInstance, this.PoolHeldItem);
		}

		private float GetTransitionTime(ItemIdentifier itemId)
		{
			ItemBase itemBase;
			if (itemId.TypeId.TryGetTemplate(out itemBase))
			{
				ThirdpersonItemBase thirdpersonModel = itemBase.ThirdpersonModel;
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
			foreach (ItemLayerLink itemLayerLink in this._itemLayers)
			{
				float num3 = num;
				ThirdpersonItemBase thirdpersonItemBase;
				if (this.TryGetCurrentInstance(out thirdpersonItemBase))
				{
					num3 *= thirdpersonItemBase.GetWeightForLayer(itemLayerLink.Layer3p).Weight;
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
			if (this._cuffedOverrideLayerIndex == null)
			{
				return;
			}
			float num = (float)(base.OwnerHub.inventory.IsDisarmed() ? 1 : 0);
			float num2 = Time.deltaTime * this._cuffedAdjustSpeed;
			float num3 = Mathf.MoveTowards(this._prevCuffWeight, num, num2);
			if (num3 != this._prevCuffWeight)
			{
				base.Animator.SetLayerWeight(this._cuffedOverrideLayerIndex.Value, num3);
				this._prevCuffWeight = num3;
			}
		}

		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			Transform boneTransform = model.Animator.GetBoneTransform(HumanBodyBones.RightHand);
			this.ItemSpawnpoint = new GameObject().transform;
			this.ItemSpawnpoint.SetParent(boneTransform, true);
			this.ItemSpawnpoint.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			this._weightAdjustSpeed = 1f;
			model.OnFadeChanged += this.OnFadeChanged;
			model.OnVisibilityChanged += this.OnVisibilityChanged;
			this.CacheLayerIndexes();
		}

		private void CacheLayerIndexes()
		{
			AnimatorLayerManager layerManager = base.Model.LayerManager;
			this._itemIdleLayerIndex = layerManager.GetLayerIndex(this._itemIdleLayer);
			this._regularIdleLayerIndex = layerManager.GetLayerIndex(this._regularIdleLayer);
			this._movementOverrideLayerIndex = layerManager.GetLayerIndex(this._movementOverrideLayer);
			this._cuffedOverrideLayerIndex = ((this._cuffedAdjustSpeed > 0f) ? new int?(layerManager.GetLayerIndex(this._cuffedOverrideLayer)) : null);
		}

		private void Update()
		{
			if (base.Model.Pooled || base.OwnerHub.isLocalPlayer)
			{
				return;
			}
			this.UpdateHeldItem(base.OwnerHub.inventory.CurItem);
			this.UpdateLayerWeights();
			this.UpdateCuffWeight();
			Action onUpdated = this.OnUpdated;
			if (onUpdated == null)
			{
				return;
			}
			onUpdated();
		}

		public bool TryGetCurrentInstance(out ThirdpersonItemBase instance)
		{
			instance = this._lastThirdpersonInstance;
			return this._hasThirdpersonInstance;
		}

		public bool TryGetCurrentInstance<T>(out T instance)
		{
			ThirdpersonItemBase thirdpersonItemBase;
			if (this.TryGetCurrentInstance(out thirdpersonItemBase) && thirdpersonItemBase is T)
			{
				T t = thirdpersonItemBase as T;
				instance = t;
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
			ThirdpersonItemBase thirdpersonItemBase;
			if (!this.TryGetCurrentInstance(out thirdpersonItemBase))
			{
				return;
			}
			thirdpersonItemBase.ReturnToPool(true);
			this._hasThirdpersonInstance = false;
		}

		public void UpdateHeldItem(ItemIdentifier itemId)
		{
			if (this._prevItem != itemId)
			{
				this._prevItem = itemId;
				this._transitionStatus = InventorySubcontroller.TransitionStatus.RetractingPrevious;
				this._weightAdjustSpeed = 2f / this.GetTransitionTime(itemId);
			}
			InventorySubcontroller.TransitionStatus transitionStatus = this._transitionStatus;
			if (transitionStatus != InventorySubcontroller.TransitionStatus.RetractingPrevious)
			{
				if (transitionStatus != InventorySubcontroller.TransitionStatus.EquippingNew)
				{
					return;
				}
				this.TransitionWeight += Time.deltaTime * this._weightAdjustSpeed;
				if (this.TransitionWeight >= 1f)
				{
					this._transitionStatus = InventorySubcontroller.TransitionStatus.Done;
				}
			}
			else
			{
				this.TransitionWeight -= Time.deltaTime * this._weightAdjustSpeed;
				if (this.TransitionWeight <= 0f)
				{
					this.SwapThirdpersonItem(itemId);
					return;
				}
			}
		}

		public LookatData ProcessLookat(LookatData original)
		{
			ILookatModifier lookatModifier;
			if (!this.TryGetCurrentInstance<ILookatModifier>(out lookatModifier))
			{
				return original;
			}
			LookatData lookatData = lookatModifier.ProcessLookat(original);
			return original.LerpTo(lookatData, this.TransitionWeight);
		}

		public HandPoseData ProcessHandPose(HandPoseData original)
		{
			IHandPoseModifier handPoseModifier;
			if (!this.TryGetCurrentInstance<IHandPoseModifier>(out handPoseModifier))
			{
				return original;
			}
			HandPoseData handPoseData = handPoseModifier.ProcessHandPose(original);
			return original.LerpTo(handPoseData, this.TransitionWeight);
		}

		private const float DefaultTransitionTime = 0.5f;

		private ItemIdentifier _prevItem;

		private Dictionary<AnimationClip, AnimationClip> _dictionarizedDefaultOverrides;

		private float _weightAdjustSpeed;

		private InventorySubcontroller.TransitionStatus _transitionStatus;

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
		private InventorySubcontroller.DefaultAnimatorOverrides[] _defaultAnimatorOverrides;

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
	}
}
