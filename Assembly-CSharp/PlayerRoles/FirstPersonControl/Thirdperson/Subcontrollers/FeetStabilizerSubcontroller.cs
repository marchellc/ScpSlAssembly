using System;
using AnimatorLayerManagement;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class FeetStabilizerSubcontroller : SubcontrollerBehaviour
{
	[Serializable]
	private class Foot
	{
		[SerializeField]
		private AvatarIKGoal _goal;

		[SerializeField]
		private HumanBodyBones _bone;

		[SerializeField]
		private Vector3 _modelspacePosition;

		[SerializeField]
		private bool _calibrate;

		private Transform _footTr;

		public void Init(Animator anim)
		{
			this._footTr = anim.GetBoneTransform(this._bone);
		}

		public void OnIK(AnimatedCharacterModel model, float weight)
		{
			Transform cachedTransform = model.CachedTransform;
			if (this._calibrate)
			{
				this._modelspacePosition = cachedTransform.InverseTransformPoint(this._footTr.position);
				return;
			}
			Vector3 goalPosition = cachedTransform.TransformPoint(this._modelspacePosition);
			model.Animator.SetIKPosition(this._goal, goalPosition);
			model.Animator.SetIKPositionWeight(this._goal, weight);
		}
	}

	[SerializeField]
	private LayerRefId _ikLayer;

	[SerializeField]
	private Foot[] _feet;

	[SerializeField]
	private float _maxWeightAdjustSpeed;

	[SerializeField]
	private float _maxWeightTurnDecreaseSpeed;

	private IRotationRetainer _rotationRetainer;

	private float _lastRetentionWeight;

	private float _maxWeight;

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		if (!model.TryGetSubcontroller<IRotationRetainer>(out this._rotationRetainer))
		{
			throw new InvalidOperationException("FeetStabilizerSubcontroller requires IRotationRetainer to operate!");
		}
		if (base.HasCuller)
		{
			base.Culler.OnAnimatorUpdated += UpdateWeight;
		}
		Foot[] feet = this._feet;
		for (int i = 0; i < feet.Length; i++)
		{
			feet[i].Init(model.Animator);
		}
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (layerIndex == base.Model.LayerManager.GetLayerIndex(this._ikLayer) && !base.Culled && (!base.HasOwner || !(base.OwnerHub.transform.localScale != Vector3.one)))
		{
			this._lastRetentionWeight = this._rotationRetainer.RetentionWeight;
			Foot[] feet = this._feet;
			for (int i = 0; i < feet.Length; i++)
			{
				feet[i].OnIK(base.Model, Mathf.Min(this._lastRetentionWeight, this._maxWeight));
			}
		}
	}

	private void UpdateWeight()
	{
		float lastMovedDeltaT = base.Model.LastMovedDeltaT;
		if (this._rotationRetainer.IsTurning)
		{
			this._maxWeight -= this._maxWeightTurnDecreaseSpeed * lastMovedDeltaT;
			if (this._maxWeight < 0f)
			{
				this._maxWeight = 0f;
			}
		}
		else
		{
			this._maxWeight = Mathf.MoveTowards(this._maxWeight, this._lastRetentionWeight, lastMovedDeltaT * this._maxWeightAdjustSpeed);
		}
	}

	private void Update()
	{
		if (!base.HasCuller)
		{
			this.UpdateWeight();
		}
	}
}
