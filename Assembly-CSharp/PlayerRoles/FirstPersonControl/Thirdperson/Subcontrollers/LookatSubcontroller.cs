using System;
using AnimatorLayerManagement;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class LookatSubcontroller : SubcontrollerBehaviour
	{
		public override void Init(AnimatedCharacterModel model, int index)
		{
			base.Init(model, index);
			this._lookPassIndex = model.LayerManager.GetLayerIndex(this._lookPassLayer);
		}

		private void OnAnimatorIK(int layerIndex)
		{
			if (base.Culled || base.ThreadmillEnabled)
			{
				return;
			}
			if (layerIndex != this._lookPassIndex)
			{
				return;
			}
			this.IKLookatPass();
		}

		private unsafe void IKLookatPass()
		{
			Vector3 forward = base.ModelTr.forward;
			Vector3 forward2 = base.Model.OwnerHub.PlayerCameraReference.forward;
			Vector3 vector = this.ClampVertical(this.ClampHorizontal(forward2, forward));
			Vector3 vector2 = new Vector3(forward.x, vector.y, forward.z).NormalizeConstrained(Vector3.up);
			float num = vector.MagnitudeOnlyY();
			Vector3 vector3 = Vector3.Slerp(vector, vector2, num);
			float num2 = this._bodyWeightOverDot.Evaluate(Vector3.Dot(forward, vector3));
			LookatData lookatData = new LookatData
			{
				LookDir = vector3,
				GlobalWeight = 1f,
				BodyWeight = num2,
				HeadWeight = 1f
			};
			ReadOnlySpan<IAnimatedModelSubcontroller> allSubcontrollers = base.Model.AllSubcontrollers;
			for (int i = 0; i < allSubcontrollers.Length; i++)
			{
				ILookatModifier lookatModifier = (*allSubcontrollers[i]) as ILookatModifier;
				if (lookatModifier != null)
				{
					lookatData = lookatModifier.ProcessLookat(lookatData);
				}
			}
			base.Animator.SetLookAtPosition(base.ModelTr.position + lookatData.LookDir * 50f);
			base.Animator.SetLookAtWeight(lookatData.GlobalWeight, lookatData.BodyWeight, lookatData.HeadWeight);
		}

		private Vector3 ClampHorizontal(Vector3 v3, Vector3 modelFwd)
		{
			float y = v3.y;
			v3.y = 0f;
			float magnitude = v3.magnitude;
			v3 /= magnitude;
			v3 = v3.ClampDot(modelFwd, 0.03f);
			return v3 * magnitude + Vector3.up * y;
		}

		private Vector3 ClampVertical(Vector3 v3)
		{
			bool flag = v3.y > 0f;
			v3.y *= (flag ? 0.65f : 0.85f);
			return v3.NormalizeConstrained(Vector3.up);
		}

		private const float LookDistance = 50f;

		private const float LookClampHorizontalDot = 0.03f;

		private const float LookClampYPositiveScale = 0.65f;

		private const float LookClampYNegativeScale = 0.85f;

		private int _lookPassIndex;

		[SerializeField]
		private AnimationCurve _bodyWeightOverDot;

		[SerializeField]
		private LayerRefId _lookPassLayer;
	}
}
