using System;
using GameObjectPools;
using InventorySystem.Items.Thirdperson;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims
{
	public class OverlayAnimationsSubcontroller : SubcontrollerBehaviour, IPoolResettable
	{
		public override void OnReassigned()
		{
			base.OnReassigned();
			OverlayAnimationsBase[] overlayAnimations = this._overlayAnimations;
			for (int i = 0; i < overlayAnimations.Length; i++)
			{
				overlayAnimations[i].OnReassigned();
			}
		}

		public void ResetObject()
		{
			OverlayAnimationsBase lastActive = this._lastActive;
			if (lastActive != null)
			{
				lastActive.OnStopped();
			}
			this._lastActive = null;
			OverlayAnimationsBase[] overlayAnimations = this._overlayAnimations;
			for (int i = 0; i < overlayAnimations.Length; i++)
			{
				overlayAnimations[i].OnReset();
			}
		}

		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			if (!model.TryGetSubcontroller<InventorySubcontroller>(out this._inventory))
			{
				throw new InvalidOperationException("Unable to setup OverlayAnimationsSubcontroller - missing dependency: InventorySubcontroller.");
			}
			this._inventory.OnUpdated += this.UpdateAll;
			for (int i = 0; i < this._overlayAnimations.Length; i++)
			{
				this._overlayAnimations[i].Init(this, i);
			}
		}

		public override void ProcessRpc(NetworkReader reader)
		{
			base.ProcessRpc(reader);
			int num = (int)reader.ReadByte();
			OverlayAnimationsBase overlayAnimationsBase;
			if (!this._overlayAnimations.TryGet(num, out overlayAnimationsBase))
			{
				return;
			}
			overlayAnimationsBase.ProcessRpc(reader);
		}

		private void OnAnimatorIK(int layerIndex)
		{
			this.ScaleIk(AvatarIKGoal.LeftHand, this._leftIkScale);
			this.ScaleIk(AvatarIKGoal.RightHand, this._rightIkScale);
		}

		private void UpdateAll()
		{
			OverlayAnimationsBase overlayAnimationsBase = null;
			foreach (OverlayAnimationsBase overlayAnimationsBase2 in this._overlayAnimations)
			{
				if (overlayAnimationsBase2.WantsToPlay)
				{
					if (!overlayAnimationsBase2.Bypassable)
					{
						overlayAnimationsBase = overlayAnimationsBase2;
						break;
					}
					if (overlayAnimationsBase == null)
					{
						overlayAnimationsBase = overlayAnimationsBase2;
					}
				}
			}
			if (overlayAnimationsBase == this._lastActive)
			{
				this.UpdateActive();
			}
			else
			{
				this.UpdateSwitch(overlayAnimationsBase);
			}
			if (this._lastActive != null)
			{
				base.Model.AnimatorOverride[this._animationToReplace] = this._lastActive.Clip;
			}
		}

		private void UpdateSwitch(OverlayAnimationsBase newMatch)
		{
			bool flag = false;
			float num = ((newMatch == null) ? 6.5f : 11.5f);
			foreach (ItemLayerLink itemLayerLink in this._layers)
			{
				if (this.AdjustLayerWeight(itemLayerLink, 0f, num) > 0f)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				OverlayAnimationsBase lastActive = this._lastActive;
				this._lastActive = newMatch;
				if (lastActive != null)
				{
					lastActive.OnStopped();
				}
				if (newMatch != null)
				{
					newMatch.OnStarted();
				}
				if (lastActive != null)
				{
					this.UpdateActive();
				}
			}
		}

		private void UpdateActive()
		{
			OverlayAnimationsBase lastActive = this._lastActive;
			if (lastActive != null)
			{
				lastActive.UpdateActive();
			}
			foreach (ItemLayerLink itemLayerLink in this._layers)
			{
				OverlayAnimationsBase lastActive2 = this._lastActive;
				float num = Mathf.Clamp01((lastActive2 != null) ? lastActive2.GetLayerWeight(itemLayerLink.Layer3p) : 0f);
				ThirdpersonItemBase thirdpersonItemBase;
				if (num > 0f && this._inventory.TryGetCurrentInstance(out thirdpersonItemBase))
				{
					ThirdpersonLayerWeight weightForLayer = thirdpersonItemBase.GetWeightForLayer(itemLayerLink.Layer3p);
					if (!weightForLayer.AllowOther)
					{
						num -= weightForLayer.Weight;
					}
				}
				this.AdjustLayerWeight(itemLayerLink, num, 6.5f);
			}
		}

		private float AdjustLayerWeight(ItemLayerLink layer, float target, float speedMultiplier)
		{
			int layerIndex = layer.GetLayerIndex(base.Model);
			float num = Mathf.MoveTowards(base.Animator.GetLayerWeight(layerIndex), target, speedMultiplier * Time.deltaTime);
			base.Animator.SetLayerWeight(layerIndex, num);
			AnimItemLayer3p layer3p = layer.Layer3p;
			if (layer3p != AnimItemLayer3p.Left)
			{
				if (layer3p == AnimItemLayer3p.Right)
				{
					this._rightIkScale = Mathf.Clamp01(1f - num);
				}
			}
			else
			{
				this._leftIkScale = Mathf.Clamp01(1f - num);
			}
			return num;
		}

		private void ScaleIk(AvatarIKGoal goal, float multiplier)
		{
			if (multiplier >= 1f)
			{
				return;
			}
			if (multiplier <= 0f)
			{
				base.Animator.SetIKPositionWeight(goal, 0f);
				base.Animator.SetIKRotationWeight(goal, 0f);
				return;
			}
			base.Animator.SetIKPositionWeight(goal, multiplier * base.Animator.GetIKPositionWeight(goal));
			base.Animator.SetIKRotationWeight(goal, multiplier * base.Animator.GetIKRotationWeight(goal));
		}

		private const float RegularAdjustSpeed = 6.5f;

		private const float DisableOldAdjustSpeed = 11.5f;

		private readonly OverlayAnimationsBase[] _overlayAnimations = new OverlayAnimationsBase[]
		{
			new FlashedAnimation(),
			new SearchCompleteAnimation()
		};

		private OverlayAnimationsBase _lastActive;

		private InventorySubcontroller _inventory;

		private float _leftIkScale;

		private float _rightIkScale;

		[SerializeField]
		private ItemLayerLink[] _layers;

		[SerializeField]
		private AnimationClip _animationToReplace;

		public AnimationClip SearchCompleteClip;

		public AnimationClip FlashedLoopClip;
	}
}
