using AnimatorLayerManagement;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Thirdperson : VariableGFXUsableItemThirdperson
{
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
		base.MainGfx.transform.GetPositionAndRotation(out var position, out var rotation);
		this._lastInstanceTr.SetPositionAndRotation(position, rotation);
		this._lastInstanceTr.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
		Vector3 lossyScale = leftHand.lossyScale;
		Vector3 position2 = this._leftHandLocalPose.position;
		Vector3 position3 = new Vector3(position2.x / lossyScale.x, position2.y / lossyScale.y, position2.z / lossyScale.z);
		this._initialInstPose = new Pose(localPosition, localRotation);
		this._targetInstPose = new Pose(position3, this._leftHandLocalPose.rotation);
		instance.SetActive(value: true);
		this._crankTr = instance.GetComponentInChildren<ConfigurableJoint>().transform;
	}

	protected override void Update()
	{
		base.Update();
		if (!base.IsUsing)
		{
			this._elapsed = 0f;
			this.SetIkSmooth(targetIsOn: true);
			return;
		}
		this._elapsed += Time.deltaTime;
		float t = Mathf.Clamp01(this._elapsed / 0.2f);
		this._lastInstanceTr.SetLocalPositionAndRotation(Vector3.Lerp(this._initialInstPose.position, this._targetInstPose.position, t), Quaternion.Lerp(this._initialInstPose.rotation, this._targetInstPose.rotation, t));
		if (this._elapsed > this._crankingPeriodMinMax.x && this._elapsed < this._crankingPeriodMinMax.y)
		{
			this.Crank();
		}
		this.SetIkSmooth(targetIsOn: false);
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
		if (layer == layerManager.GetLayerIndex(this._leftHandIkLayer))
		{
			Animator animator = base.TargetModel.Animator;
			this._leftHandIkTarget.GetPositionAndRotation(out var position, out var rotation);
			animator.SetIKPosition(AvatarIKGoal.LeftHand, position);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, this._ikBlend);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, rotation);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, this._ikBlend);
		}
	}

	private void SetIkSmooth(bool targetIsOn)
	{
		this._ikBlend = Mathf.MoveTowards(this._ikBlend, targetIsOn ? 1 : 0, Time.deltaTime * 4f);
	}
}
