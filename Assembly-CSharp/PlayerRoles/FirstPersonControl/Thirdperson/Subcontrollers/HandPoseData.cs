using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	[Serializable]
	public struct HandPoseData
	{
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
			return new HandPoseData
			{
				LeftHandPose = Mathf.Lerp(this.LeftHandPose, target.LeftHandPose, weight),
				LeftHandWeight = Mathf.Lerp(this.LeftHandWeight, target.LeftHandWeight, weight),
				RightHandPose = Mathf.Lerp(this.RightHandPose, target.RightHandPose, weight),
				RightHandWeight = Mathf.Lerp(this.RightHandWeight, target.RightHandWeight, weight)
			};
		}

		public float LeftHandWeight;

		public float LeftHandPose;

		public float RightHandWeight;

		public float RightHandPose;
	}
}
