using System;
using AnimatorLayerManagement;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class HandPoseSubcontroller : SubcontrollerBehaviour
{
	private static readonly int LeftHandPoseHash = Animator.StringToHash("LeftHandPose");

	private static readonly int RightHandPoseHash = Animator.StringToHash("RightHandPose");

	private int _leftHandLayerIndex;

	private int _rightHandLayerIndex;

	[SerializeField]
	private LayerRefId _leftHandLayer;

	[SerializeField]
	private LayerRefId _rightHandLayer;

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		if (base.HasCuller)
		{
			base.Culler.OnBeforeAnimatorUpdated += Evaluate;
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

	private void Evaluate()
	{
		HandPoseData data = default(HandPoseData);
		ReadOnlySpan<IAnimatedModelSubcontroller> allSubcontrollers = base.Model.AllSubcontrollers;
		for (int i = 0; i < allSubcontrollers.Length; i++)
		{
			if (allSubcontrollers[i] is IHandPoseModifier handPoseModifier)
			{
				data = handPoseModifier.ProcessHandPose(data);
			}
		}
		base.Animator.SetLayerWeight(this._leftHandLayerIndex, data.LeftHandWeight);
		base.Animator.SetLayerWeight(this._rightHandLayerIndex, data.RightHandWeight);
		base.Animator.SetFloat(HandPoseSubcontroller.LeftHandPoseHash, data.LeftHandPose);
		base.Animator.SetFloat(HandPoseSubcontroller.RightHandPoseHash, data.RightHandPose);
	}
}
