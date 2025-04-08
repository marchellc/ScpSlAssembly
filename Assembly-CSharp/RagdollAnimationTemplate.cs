using System;
using PlayerRoles.Ragdolls;
using UnityEngine;

public class RagdollAnimationTemplate : MonoBehaviour
{
	public void ProcessRagdoll(BasicRagdoll rg)
	{
		DynamicRagdoll dynamicRagdoll = rg as DynamicRagdoll;
		if (dynamicRagdoll == null)
		{
			return;
		}
		int num = Mathf.Min(this._bones.Length, dynamicRagdoll.LinkedRigidbodies.Length);
		rg.transform.rotation *= this._overallRotation;
		for (int i = 0; i < num; i++)
		{
			Rigidbody rigidbody = dynamicRagdoll.LinkedRigidbodies[i];
			RagdollAnimationTemplate.RagdollBone ragdollBone = this._bones[i];
			Transform transform = rigidbody.transform;
			transform.localRotation = ragdollBone.RotationOffset;
			transform.position = rg.Info.StartPosition + rg.Info.StartRotation * ragdollBone.PositionOffset;
			rigidbody.velocity = rg.Info.StartRotation * ragdollBone.StartVelocity;
		}
	}

	[SerializeField]
	private RagdollAnimationTemplate.RagdollBone[] _bones;

	[SerializeField]
	private Quaternion _overallRotation;

	[Serializable]
	private struct RagdollBone
	{
		public Vector3 PositionOffset;

		public Quaternion RotationOffset;

		public Vector3 StartVelocity;
	}
}
