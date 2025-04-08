using System;
using InventorySystem.Items.Firearms.Extensions;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Thirdperson
{
	[Serializable]
	public class LeftHandIKHandler : IHandPoseModifier
	{
		public HandPoseData ProcessHandPose(HandPoseData data)
		{
			foreach (LeftHandIKHandler.Anchor anchor in this._anchors)
			{
				if (anchor.Condition.Evaluate())
				{
					data.LeftHandPose = anchor.PoseTime;
					data.LeftHandWeight = anchor.PoseOverride;
					break;
				}
			}
			return data;
		}

		public void Initialize(FirearmWorldmodel woldmodel, AnimatedCharacterModel model)
		{
			this._anim = model.Animator;
			LeftHandIKHandler.Anchor[] anchors = this._anchors;
			for (int i = 0; i < anchors.Length; i++)
			{
				anchors[i].Condition.InitWorldmodel(woldmodel);
			}
		}

		public void IKUpdateLeftHandAnchor(float ikScale)
		{
			bool flag = false;
			foreach (LeftHandIKHandler.Anchor anchor in this._anchors)
			{
				if (anchor.Condition.Evaluate())
				{
					this._anim.SetIKPosition(AvatarIKGoal.LeftHand, anchor.Point.position);
					this._anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikScale);
					this._anim.SetIKRotation(AvatarIKGoal.LeftHand, anchor.Point.rotation);
					this._anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikScale);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this._anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
				this._anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
			}
		}

		private Animator _anim;

		[SerializeField]
		private LeftHandIKHandler.Anchor[] _anchors;

		[Serializable]
		private struct Anchor
		{
			public Transform Point;

			public float PoseTime;

			public float PoseOverride;

			public ConditionalEvaluator Condition;
		}
	}
}
