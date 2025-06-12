using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Searching;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class InstantStatusSubEffect : HypothermiaSubEffectBase, IWeaponModifierPlayerEffect, ISearchTimeModifier, IMovementSpeedModifier
{
	private readonly Dictionary<AttachmentParam, float> _dictionarized = new Dictionary<AttachmentParam, float>();

	private float _currentIntensity;

	private float _statsPrevIntensity;

	[SerializeField]
	private float _decaySpeed;

	[SerializeField]
	private float _maxExposure;

	[SerializeField]
	private float _movementSpeedMultiplier;

	[SerializeField]
	private float _searchTimeAdditionIncrease;

	[SerializeField]
	private float _searchTimeMultiplierIncrease;

	[SerializeField]
	private AttachmentParameterValuePair[] _weaponStats;

	private float CurIntensity => this._currentIntensity * RainbowTaste.CurrentMultiplier(base.Hub);

	private float VitalityMultiplier => (!Vitality.CheckPlayer(base.Hub)) ? 1 : 0;

	public override bool IsActive => this._currentIntensity > 0f;

	public bool ParamsActive => true;

	public bool MovementModifierActive => true;

	public float MovementSpeedMultiplier => Mathf.LerpUnclamped(1f, this._movementSpeedMultiplier, this.VitalityMultiplier * this.CurIntensity);

	public float MovementSpeedLimit => float.MaxValue;

	public float ProcessSearchTime(float val)
	{
		float num = Mathf.LerpUnclamped(1f, this._searchTimeMultiplierIncrease, this.CurIntensity);
		return val * num + this._searchTimeAdditionIncrease * this.CurIntensity;
	}

	internal override void UpdateEffect(float curExposure)
	{
		this._currentIntensity -= this._decaySpeed * Time.deltaTime;
		if (this._currentIntensity < curExposure)
		{
			this._currentIntensity = curExposure;
		}
		if (this._currentIntensity > this._maxExposure)
		{
			this._currentIntensity = this._maxExposure;
		}
		float num = this.CurIntensity * this.VitalityMultiplier;
		if (num != this._statsPrevIntensity)
		{
			AttachmentParameterValuePair[] weaponStats = this._weaponStats;
			for (int i = 0; i < weaponStats.Length; i++)
			{
				AttachmentParameterValuePair attachmentParameterValuePair = weaponStats[i];
				this._dictionarized[attachmentParameterValuePair.Parameter] = Mathf.LerpUnclamped(1f, attachmentParameterValuePair.Value, num);
			}
			this._statsPrevIntensity = num;
		}
	}

	public override void DisableEffect()
	{
		this._currentIntensity = 0f;
		this._dictionarized.Clear();
	}

	public bool TryGetWeaponParam(AttachmentParam param, out float val)
	{
		return this._dictionarized.TryGetValue(param, out val);
	}
}
