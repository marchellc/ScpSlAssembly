using System;
using InventorySystem.Items.Thirdperson;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Coin
{
	public class CoinThirdperson : IdleThirdpersonItem, ILookatModifier
	{
		internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
		{
			base.Initialize(subcontroller, id);
			this._lastThrowTime = 0.0;
			Coin.OnFlipped += this.OnCoinflip;
			GameObject characterModelTemplate = subcontroller.Model.LastRole.FpcModule.CharacterModelTemplate;
			this._heightOffset = 0f;
			foreach (CoinThirdperson.ModelHeightOffset modelHeightOffset in this._modelSpecificResultOffsets)
			{
				if (!(modelHeightOffset.ModelTemplate != characterModelTemplate))
				{
					this._heightOffset = modelHeightOffset.HeightOffset;
					return;
				}
			}
		}

		public LookatData ProcessLookat(LookatData data)
		{
			data.GlobalWeight *= this._lastIkMultiplier;
			return data;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			Coin.OnFlipped -= this.OnCoinflip;
		}

		private void LateUpdate()
		{
			float num = (float)(NetworkTime.time - this._lastThrowTime);
			float num2 = this._resultBlendAnimation.Evaluate(num);
			Vector3 vector;
			Quaternion quaternion;
			this._defaultPose.GetPositionAndRotation(out vector, out quaternion);
			if (num2 > 0f)
			{
				Vector3 vector2;
				Quaternion quaternion2;
				(this._lastTails ? this._tailsResultPose : this._headsResultPose).GetPositionAndRotation(out vector2, out quaternion2);
				vector = Vector3.Lerp(vector, vector2 + Vector3.up * this._heightOffset, num2);
				quaternion = Quaternion.Lerp(quaternion, quaternion2, num2);
			}
			vector += Vector3.up * this._heightAnimation.Evaluate(num);
			this._coinTr.SetPositionAndRotation(vector, quaternion);
			this._coinTr.Rotate(this._rotationAxis * this._rotationAnimation.Evaluate(num), Space.Self);
			this._lastIkMultiplier = this._ikMultiplierAnimation.Evaluate(num);
		}

		private void OnCoinflip(ushort serial, bool isTails)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this._lastTails = isTails;
			this._lastThrowTime = NetworkTime.time;
			base.SetAnim(AnimState3p.Override0, this._throwAnim);
			base.ReplayOverrideBlend(true);
		}

		[SerializeField]
		private AnimationCurve _resultBlendAnimation;

		[SerializeField]
		private AnimationCurve _heightAnimation;

		[SerializeField]
		private AnimationCurve _rotationAnimation;

		[SerializeField]
		private AnimationCurve _ikMultiplierAnimation;

		[SerializeField]
		private Vector3 _rotationAxis;

		[SerializeField]
		private Transform _coinTr;

		[SerializeField]
		private AnimationClip _throwAnim;

		[SerializeField]
		private Transform _headsResultPose;

		[SerializeField]
		private Transform _tailsResultPose;

		[SerializeField]
		private Transform _defaultPose;

		[SerializeField]
		private CoinThirdperson.ModelHeightOffset[] _modelSpecificResultOffsets;

		private double _lastThrowTime;

		private bool _lastTails;

		private float _heightOffset;

		private float _lastIkMultiplier;

		[Serializable]
		private struct ModelHeightOffset
		{
			public GameObject ModelTemplate;

			public float HeightOffset;
		}
	}
}
