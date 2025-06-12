using InventorySystem.Items.Thirdperson;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public class ThrowableItemThirdperson : ThirdpersonItemBase, IHandPoseModifier, ILookatModifier
{
	[SerializeField]
	private AnimationClip _idleClip;

	[SerializeField]
	private AnimationClip _beginClip;

	[SerializeField]
	private AnimationClip _throwClip;

	[SerializeField]
	private HandPoseData _rightHandPose;

	[SerializeField]
	private GameObject _gfxToDisable;

	[SerializeField]
	private HybridLayerProcessor _layerProcessor;

	private float _targetWeight;

	private float _remainingActiveTime;

	private bool _isThrowing;

	private float _lastWeight;

	private const float WeightIncreaseSpeed = 8.5f;

	private const float WeightDecreaseSpeed = 3.5f;

	private const float ThrowingTime = 0.22f;

	public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
	{
		return this._layerProcessor.GetWeightForLayer(this, layer);
	}

	public virtual HandPoseData ProcessHandPose(HandPoseData data)
	{
		return this._rightHandPose;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._targetWeight = 0f;
		this._lastWeight = 0f;
		this._isThrowing = false;
		this._gfxToDisable.SetActive(value: true);
	}

	internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		base.Initialize(subcontroller, id);
		base.SetAnim(AnimState3p.Override0, this._idleClip);
	}

	protected override void Update()
	{
		base.Update();
		if (!base.Pooled)
		{
			if (this._lastWeight != this._targetWeight)
			{
				float num = ((this._targetWeight > this._lastWeight) ? 8.5f : 3.5f);
				float num2 = Mathf.MoveTowards(this._lastWeight, this._targetWeight, Time.deltaTime * num);
				this._layerProcessor.SetDualHandBlend(num2);
				this._lastWeight = num2;
				base.OverrideBlend = num2;
			}
			if (this._isThrowing)
			{
				this._remainingActiveTime -= Time.deltaTime;
				this._gfxToDisable.SetActive(this._remainingActiveTime > 0f);
			}
		}
	}

	private void Awake()
	{
		ThrowableNetworkHandler.OnAudioMessageReceived += ProcessRequest;
	}

	private void OnDestroy()
	{
		ThrowableNetworkHandler.OnAudioMessageReceived -= ProcessRequest;
	}

	private void ProcessRequest(ThrowableNetworkHandler.ThrowableItemAudioMessage msg)
	{
		if (!base.Pooled && base.ItemId.SerialNumber == msg.Serial)
		{
			switch (msg.Request)
			{
			case ThrowableNetworkHandler.RequestType.BeginThrow:
				this.PlayAnim(this._beginClip);
				break;
			case ThrowableNetworkHandler.RequestType.CancelThrow:
				this._targetWeight = 0f;
				break;
			case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
			case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
				this.PlayAnim(this._throwClip);
				this._remainingActiveTime = 0.22f;
				this._isThrowing = true;
				break;
			}
		}
	}

	private void PlayAnim(AnimationClip clip)
	{
		this._targetWeight = 1f;
		base.SetAnim(AnimState3p.Override1, clip);
		base.ReplayOverrideBlend(soft: false);
	}

	public LookatData ProcessLookat(LookatData data)
	{
		data.BodyWeight = Mathf.Clamp01(data.BodyWeight + this._lastWeight);
		return data;
	}
}
