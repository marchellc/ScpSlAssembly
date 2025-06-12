using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Jailbird;

public class JailbirdThirdperson : ThirdpersonItemBase
{
	private AnimState3p _targetAnim;

	[SerializeField]
	private AnimationClip _idleAnim;

	[SerializeField]
	private AnimationClip _chargingAnim;

	[SerializeField]
	private AnimationClip _swingAnim;

	[SerializeField]
	private AnimationClip _chargeHitAnim;

	[SerializeField]
	private AudioClip _attackSound;

	[SerializeField]
	private AudioClip _chargeSound;

	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private float _blendAdjustSpeed;

	[SerializeField]
	private JailbirdMaterialController _materialController;

	[SerializeField]
	private GameObject _chargeLoadParticles;

	[SerializeField]
	private GameObject _chargingParticles;

	public override void ResetObject()
	{
		base.ResetObject();
		JailbirdItem.OnRpcReceived -= OnRpcReceived;
		this._chargeLoadParticles.SetActive(value: false);
		this._chargingParticles.SetActive(value: false);
	}

	internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		base.Initialize(subcontroller, id);
		this._materialController.SetSerial(id.SerialNumber);
		base.SetAnim(AnimState3p.Override0, this._idleAnim);
		base.SetAnim(AnimState3p.Override1, this._chargingAnim);
		this._targetAnim = AnimState3p.Override0;
		base.OverrideBlend = 0f;
		JailbirdItem.OnRpcReceived += OnRpcReceived;
	}

	public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
	{
		return new ThirdpersonLayerWeight(1f, layer != AnimItemLayer3p.Right && this._targetAnim == AnimState3p.Override0);
	}

	protected override void Update()
	{
		base.Update();
		base.OverrideBlend = Mathf.MoveTowards(base.OverrideBlend, (float)this._targetAnim, Time.deltaTime * this._blendAdjustSpeed);
	}

	private void OnRpcReceived(ushort serial, JailbirdMessageType rpc)
	{
		if (serial != base.ItemId.SerialNumber)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		switch (rpc)
		{
		default:
			return;
		case JailbirdMessageType.AttackPerformed:
			if (this._targetAnim == AnimState3p.Override1)
			{
				this.PlayAttack(this._chargeHitAnim);
				break;
			}
			return;
		case JailbirdMessageType.AttackTriggered:
			this.PlayAttack(this._swingAnim);
			break;
		case JailbirdMessageType.ChargeFailed:
			this._targetAnim = AnimState3p.Override0;
			break;
		case JailbirdMessageType.ChargeLoadTriggered:
			this._targetAnim = AnimState3p.Override1;
			flag = true;
			break;
		case JailbirdMessageType.ChargeStarted:
			this._targetAnim = AnimState3p.Override1;
			flag2 = true;
			this._audioSource.PlayOneShot(this._chargeSound);
			break;
		}
		this._chargingParticles.SetActive(flag2);
		this._chargeLoadParticles.SetActive(flag || flag2);
	}

	private void PlayAttack(AnimationClip clip)
	{
		this._targetAnim = AnimState3p.Override0;
		base.SetAnim(this._targetAnim, clip);
		base.ReplayOverrideBlend(soft: false);
		this._audioSource.Stop();
		this._audioSource.PlayOneShot(this._attackSound);
	}
}
