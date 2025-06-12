using System;
using PlayerRoles.Ragdolls;
using UnityEngine;

public class RagdollAnimationTemplate : MonoBehaviour
{
	[Serializable]
	private struct RagdollBone
	{
		public Vector3 PositionOffset;

		public Quaternion RotationOffset;

		public Vector3 StartVelocity;
	}

	[SerializeField]
	private RagdollBone[] _bones;

	[SerializeField]
	private Quaternion _overallRotation;

	public void ProcessRagdoll(BasicRagdoll rg)
	{
		if (rg is DynamicRagdoll dynamicRagdoll)
		{
			int num = Mathf.Min(this._bones.Length, dynamicRagdoll.LinkedRigidbodies.Length);
			rg.transform.rotation *= this._overallRotation;
			for (int i = 0; i < num; i++)
			{
				Rigidbody obj = dynamicRagdoll.LinkedRigidbodies[i];
				RagdollBone ragdollBone = this._bones[i];
				Transform obj2 = obj.transform;
				obj2.localRotation = ragdollBone.RotationOffset;
				obj2.position = rg.Info.StartPosition + rg.Info.StartRotation * ragdollBone.PositionOffset;
				obj.linearVelocity = rg.Info.StartRotation * ragdollBone.StartVelocity;
			}
		}
	}
}
