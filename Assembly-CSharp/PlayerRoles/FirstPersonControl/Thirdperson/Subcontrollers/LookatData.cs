using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

[Serializable]
public struct LookatData
{
	public Vector3 LookDir;

	public float GlobalWeight;

	public float BodyWeight;

	public float HeadWeight;

	public LookatData LerpTo(LookatData target, float weight)
	{
		if (weight <= 0f)
		{
			return this;
		}
		if (weight >= 1f)
		{
			return target;
		}
		return new LookatData
		{
			LookDir = Vector3.Slerp(this.LookDir, target.LookDir, weight),
			GlobalWeight = Mathf.Lerp(this.GlobalWeight, target.GlobalWeight, weight),
			BodyWeight = Mathf.Lerp(this.BodyWeight, target.BodyWeight, weight),
			HeadWeight = Mathf.Lerp(this.HeadWeight, target.HeadWeight, weight)
		};
	}
}
