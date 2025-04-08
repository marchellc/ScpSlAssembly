using System;
using AnimatorLayerManagement;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1576
{
	public class Scp1576Thirdperson : VariableGFXUsableItemThirdperson
	{
		internal override void OnAnimIK(int layerIndex, float ikScale)
		{
			base.OnAnimIK(layerIndex, ikScale);
			if (this._ikBlend > 0f)
			{
				this.UpdateIdleIK(layerIndex);
			}
		}

		protected override void SetupLeftHandedInstance(GameObject instance, Transform leftHand)
		{
			this._lastInstanceTr = instance.transform;
			this._lastInstanceTr.SetParent(leftHand);
			Vector3 vector;
			Quaternion quaternion;
			base.MainGfx.transform.GetPositionAndRotation(out vector, out quaternion);
			this._lastInstanceTr.SetPositionAndRotation(vector, quaternion);
			Vector3 vector2;
			Quaternion quaternion2;
			this._lastInstanceTr.GetLocalPositionAndRotation(out vector2, out quaternion2);
			Vector3 lossyScale = leftHand.lossyScale;
			Vector3 position = this._leftHandLocalPose.position;
			Vector3 vector3 = new Vector3(position.x / lossyScale.x, position.y / lossyScale.y, position.z / lossyScale.z);
			this._initialInstPose = new Pose(vector2, quaternion2);
			this._targetInstPose = new Pose(vector3, this._leftHandLocalPose.rotation);
			instance.SetActive(true);
			this._crankTr = instance.GetComponentInChildren<ConfigurableJoint>().transform;
		}

		protected override void Update()
		{
			base.Update();
			if (!base.IsUsing)
			{
				this._elapsed = 0f;
				this.SetIkSmooth(true);
				return;
			}
			this._elapsed += Time.deltaTime;
			float num = Mathf.Clamp01(this._elapsed / 0.2f);
			this._lastInstanceTr.SetLocalPositionAndRotation(Vector3.Lerp(this._initialInstPose.position, this._targetInstPose.position, num), Quaternion.Lerp(this._initialInstPose.rotation, this._targetInstPose.rotation, num));
			if (this._elapsed > this._crankingPeriodMinMax.x && this._elapsed < this._crankingPeriodMinMax.y)
			{
				this.Crank();
			}
			this.SetIkSmooth(false);
			if (this._elapsed > this._animEndTime)
			{
				base.IsUsing = false;
				this.OnUsingStatusChanged();
			}
		}

		private bool Crank()
		{
			Vector3 vector = this._crankAnchor.position - this._crankTr.position;
			Vector3 vector2 = this._crankTr.parent.TransformDirection(this._crankParentUp);
			Quaternion quaternion = Quaternion.LookRotation(Vector3.ProjectOnPlane(vector, vector2), vector2);
			Quaternion quaternion2 = Quaternion.FromToRotation(this._crankForward, Vector3.forward);
			this._crankTr.rotation = quaternion * quaternion2;
			return true;
		}

		private void UpdateIdleIK(int layer)
		{
			AnimatorLayerManager layerManager = base.TargetModel.LayerManager;
			if (layer != layerManager.GetLayerIndex(this._leftHandIkLayer))
			{
				return;
			}
			Animator animator = base.TargetModel.Animator;
			Vector3 vector;
			Quaternion quaternion;
			this._leftHandIkTarget.GetPositionAndRotation(out vector, out quaternion);
			animator.SetIKPosition(AvatarIKGoal.LeftHand, vector);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, this._ikBlend);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, quaternion);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, this._ikBlend);
		}

		private void SetIkSmooth(bool targetIsOn)
		{
			this._ikBlend = Mathf.MoveTowards(this._ikBlend, (float)(targetIsOn ? 1 : 0), Time.deltaTime * 4f);
		}

		[SerializeField]
		private Transform _leftHandIkTarget;

		[SerializeField]
		private LayerRefId _leftHandIkLayer;

		[SerializeField]
		private Pose _leftHandLocalPose;

		[SerializeField]
		private Vector2 _crankingPeriodMinMax;

		[SerializeField]
		private Transform _crankAnchor;

		[SerializeField]
		private Vector3 _crankForward;

		[SerializeField]
		private Vector3 _crankParentUp;

		[SerializeField]
		private float _animEndTime;

		private float _elapsed;

		private Transform _crankTr;

		private Transform _lastInstanceTr;

		private Pose _initialInstPose;

		private Pose _targetInstPose;

		private float _ikBlend;

		private const float IkBlendSpeed = 4f;

		private const float PoseAdjustDuration = 0.2f;
	}
}
