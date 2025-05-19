using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

[Serializable]
public struct HandPoseData
{
	public float LeftHandWeight;

	public float LeftHandPose;

	public float RightHandWeight;

	public float RightHandPose;

	public HandPoseData LerpTo(HandPoseData target, float weight)
	{
		if (weight <= 0f)
		{
			return this;
		}
		if (weight >= 1f)
		{
			return target;
		}
		HandPoseData result = default(HandPoseData);
		result.LeftHandPose = Mathf.Lerp(LeftHandPose, target.LeftHandPose, weight);
		result.LeftHandWeight = Mathf.Lerp(LeftHandWeight, target.LeftHandWeight, weight);
		result.RightHandPose = Mathf.Lerp(RightHandPose, target.RightHandPose, weight);
		result.RightHandWeight = Mathf.Lerp(RightHandWeight, target.RightHandWeight, weight);
		return result;
	}
}
