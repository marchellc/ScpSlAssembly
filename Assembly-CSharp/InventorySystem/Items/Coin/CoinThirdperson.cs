using System;
using InventorySystem.Items.Thirdperson;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Coin;

public class CoinThirdperson : IdleThirdpersonItem, ILookatModifier
{
	[Serializable]
	private struct ModelHeightOffset
	{
		public GameObject ModelTemplate;

		public float HeightOffset;
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
	private ModelHeightOffset[] _modelSpecificResultOffsets;

	private double _lastThrowTime;

	private bool _lastTails;

	private float _heightOffset;

	private float _lastIkMultiplier;

	internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		base.Initialize(subcontroller, id);
		this._lastThrowTime = 0.0;
		Coin.OnFlipped += OnCoinflip;
		GameObject characterModelTemplate = subcontroller.Model.LastRole.FpcModule.CharacterModelTemplate;
		this._heightOffset = 0f;
		ModelHeightOffset[] modelSpecificResultOffsets = this._modelSpecificResultOffsets;
		for (int i = 0; i < modelSpecificResultOffsets.Length; i++)
		{
			ModelHeightOffset modelHeightOffset = modelSpecificResultOffsets[i];
			if (!(modelHeightOffset.ModelTemplate != characterModelTemplate))
			{
				this._heightOffset = modelHeightOffset.HeightOffset;
				break;
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
		Coin.OnFlipped -= OnCoinflip;
	}

	private void LateUpdate()
	{
		float time = (float)(NetworkTime.time - this._lastThrowTime);
		float num = this._resultBlendAnimation.Evaluate(time);
		this._defaultPose.GetPositionAndRotation(out var position, out var rotation);
		if (num > 0f)
		{
			(this._lastTails ? this._tailsResultPose : this._headsResultPose).GetPositionAndRotation(out var position2, out var rotation2);
			position = Vector3.Lerp(position, position2 + Vector3.up * this._heightOffset, num);
			rotation = Quaternion.Lerp(rotation, rotation2, num);
		}
		position += Vector3.up * this._heightAnimation.Evaluate(time);
		this._coinTr.SetPositionAndRotation(position, rotation);
		this._coinTr.Rotate(this._rotationAxis * this._rotationAnimation.Evaluate(time), Space.Self);
		this._lastIkMultiplier = this._ikMultiplierAnimation.Evaluate(time);
	}

	private void OnCoinflip(ushort serial, bool isTails)
	{
		if (serial == base.ItemId.SerialNumber)
		{
			this._lastTails = isTails;
			this._lastThrowTime = NetworkTime.time;
			base.SetAnim(AnimState3p.Override0, this._throwAnim);
			base.ReplayOverrideBlend(soft: true);
		}
	}
}
