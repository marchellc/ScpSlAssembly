using System;
using InventorySystem.Items.Firearms.Extensions;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Thirdperson
{
	[Serializable]
	public class RightHandIKHandler : IHandPoseModifier
	{
		public HandPoseData ProcessHandPose(HandPoseData data)
		{
			RightHandIKHandler.RightHandSettings curSettings = this.GetCurSettings(this._lastAdsBlend);
			data.RightHandPose = curSettings.PoseTime;
			data.RightHandWeight = curSettings.PoseWeight;
			return data;
		}

		public void Initialize(FirearmWorldmodel woldmodel, AnimatedCharacterModel model)
		{
			this._model = model;
			RightHandIKHandler.RightHandSettings[] array = this._adsSettings;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Condition.InitWorldmodel(woldmodel);
			}
			array = this._hipSettings;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Condition.InitWorldmodel(woldmodel);
			}
		}

		public void IKUpdateRightHandRotation(float ikScale, float adsBlend)
		{
			this._lastAdsBlend = adsBlend;
			Animator animator = this._model.Animator;
			Transform playerCameraReference = this._model.OwnerHub.PlayerCameraReference;
			Transform boneTransform = animator.GetBoneTransform(HumanBodyBones.RightHand);
			Vector3 position = MainCameraController.CurrentCamera.position;
			Vector3 vector = playerCameraReference.forward.NormalizeIgnoreY();
			Vector2 vector2 = new Vector2(position.x, position.z);
			Vector2 vector3 = new Vector2(playerCameraReference.position.x, playerCameraReference.position.z);
			Vector2 vector4 = new Vector2(vector.x, vector.z);
			float num = Vector2.Dot(vector2 - vector3, vector4);
			float sqrMagnitude = (vector3 + num * vector4 - vector2).sqrMagnitude;
			Vector2 vector5;
			if (sqrMagnitude > 0.1f || num < 0f)
			{
				vector5 = Vector2.zero;
			}
			else
			{
				float num2 = Mathf.InverseLerp(0.1f, 0.04f, sqrMagnitude);
				float num3 = Vector2.Distance(vector2, vector3);
				vector5 = this.CalculateAimCorrection(num2, num3, playerCameraReference, boneTransform);
			}
			float num4 = 9f * this._model.LastMovedDeltaT;
			Vector2 vector6 = Vector2.Lerp(this._prevCorrection, vector5, num4);
			this.ApplyIKRotation(playerCameraReference, vector6, ikScale);
			this.ApplyIKPosition(playerCameraReference, boneTransform, ikScale);
			this._prevCorrection = vector6;
		}

		private Vector2 CalculateAimCorrection(float blend, float aimDistance, Transform cam, Transform rightHand)
		{
			if (aimDistance < 0.05f)
			{
				aimDistance = 1.2f;
				blend = 0f;
			}
			else if (aimDistance < 1.2f)
			{
				aimDistance = 1.2f;
			}
			Vector3 vector = cam.InverseTransformPoint(rightHand.position);
			Vector2 vector2 = new Vector2(Mathf.Atan2(aimDistance, vector.x) * 57.29578f - 90f, Mathf.Atan2(aimDistance, vector.y) * 57.29578f - 90f);
			return this._aimCorrectionIntensity * blend * vector2;
		}

		private void ApplyIKRotation(Transform cam, Vector2 correction, float ikScale)
		{
			Quaternion quaternion = cam.rotation * Quaternion.Euler(this._handRotation);
			Quaternion quaternion2 = Quaternion.Euler(-correction.x, -correction.y / 2f, 0f) * quaternion;
			this._model.Animator.SetIKRotation(AvatarIKGoal.RightHand, quaternion2);
			this._model.Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikScale);
		}

		private void ApplyIKPosition(Transform cam, Transform rightHand, float ikScale)
		{
			RightHandIKHandler.RightHandSettings curSettings = this.GetCurSettings(this._lastAdsBlend);
			if (curSettings.IKPositionWeight > 0f)
			{
				this._model.Animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.position + cam.TransformDirection(curSettings.IKPosition));
				this._model.Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, curSettings.IKPositionWeight * ikScale);
			}
		}

		private RightHandIKHandler.RightHandSettings GetCurSettings(float adsBlend)
		{
			RightHandIKHandler.RightHandSettings? rightHandSettings = null;
			RightHandIKHandler.RightHandSettings? rightHandSettings2 = null;
			foreach (RightHandIKHandler.RightHandSettings rightHandSettings3 in this._hipSettings)
			{
				if (rightHandSettings3.Condition.Evaluate())
				{
					rightHandSettings = new RightHandIKHandler.RightHandSettings?(rightHandSettings3);
					break;
				}
			}
			foreach (RightHandIKHandler.RightHandSettings rightHandSettings4 in this._adsSettings)
			{
				if (rightHandSettings4.Condition.Evaluate())
				{
					rightHandSettings2 = new RightHandIKHandler.RightHandSettings?(rightHandSettings4);
					break;
				}
			}
			return rightHandSettings.GetValueOrDefault().LerpTo(rightHandSettings2.GetValueOrDefault(), adsBlend);
		}

		private const float MinAimDistance = 1.2f;

		private const float IgnoreAimDistance = 0.05f;

		private const float CorrectionLerpSpeed = 9f;

		private Vector2 _prevCorrection;

		private float _lastAdsBlend;

		private AnimatedCharacterModel _model;

		[SerializeField]
		private Vector3 _handRotation;

		[SerializeField]
		private RightHandIKHandler.RightHandSettings[] _hipSettings;

		[SerializeField]
		private RightHandIKHandler.RightHandSettings[] _adsSettings;

		[SerializeField]
		private float _aimCorrectionIntensity;

		[Serializable]
		private struct RightHandSettings
		{
			public RightHandIKHandler.RightHandSettings LerpTo(RightHandIKHandler.RightHandSettings target, float weight)
			{
				if (weight <= 0f)
				{
					return this;
				}
				if (weight >= 1f)
				{
					return target;
				}
				return new RightHandIKHandler.RightHandSettings
				{
					IKPositionWeight = Mathf.Lerp(this.IKPositionWeight, target.IKPositionWeight, weight),
					IKPosition = Vector3.Lerp(this.IKPosition, target.IKPosition, weight),
					PoseTime = Mathf.Lerp(this.PoseTime, target.PoseTime, weight),
					PoseWeight = Mathf.Lerp(this.PoseWeight, target.PoseWeight, weight)
				};
			}

			public float IKPositionWeight;

			public Vector3 IKPosition;

			public float PoseTime;

			public float PoseWeight;

			public ConditionalEvaluator Condition;
		}
	}
}
