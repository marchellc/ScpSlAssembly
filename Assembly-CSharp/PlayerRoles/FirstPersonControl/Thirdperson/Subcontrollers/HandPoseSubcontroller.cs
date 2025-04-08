using System;
using AnimatorLayerManagement;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class HandPoseSubcontroller : SubcontrollerBehaviour
	{
		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			if (base.HasCuller)
			{
				base.Culler.OnBeforeAnimatorUpdated += this.Evaluate;
			}
			this._leftHandLayerIndex = model.LayerManager.GetLayerIndex(this._leftHandLayer);
			this._rightHandLayerIndex = model.LayerManager.GetLayerIndex(this._rightHandLayer);
		}

		private void LateUpdate()
		{
			if (!base.HasCuller)
			{
				this.Evaluate();
			}
		}

		private unsafe void Evaluate()
		{
			HandPoseData handPoseData = default(HandPoseData);
			ReadOnlySpan<IAnimatedModelSubcontroller> allSubcontrollers = base.Model.AllSubcontrollers;
			for (int i = 0; i < allSubcontrollers.Length; i++)
			{
				IHandPoseModifier handPoseModifier = (*allSubcontrollers[i]) as IHandPoseModifier;
				if (handPoseModifier != null)
				{
					handPoseData = handPoseModifier.ProcessHandPose(handPoseData);
				}
			}
			base.Animator.SetLayerWeight(this._leftHandLayerIndex, handPoseData.LeftHandWeight);
			base.Animator.SetLayerWeight(this._rightHandLayerIndex, handPoseData.RightHandWeight);
			base.Animator.SetFloat(HandPoseSubcontroller.LeftHandPoseHash, handPoseData.LeftHandPose);
			base.Animator.SetFloat(HandPoseSubcontroller.RightHandPoseHash, handPoseData.RightHandPose);
		}

		private static readonly int LeftHandPoseHash = Animator.StringToHash("LeftHandPose");

		private static readonly int RightHandPoseHash = Animator.StringToHash("RightHandPose");

		private int _leftHandLayerIndex;

		private int _rightHandLayerIndex;

		[SerializeField]
		private LayerRefId _leftHandLayer;

		[SerializeField]
		private LayerRefId _rightHandLayer;
	}
}
