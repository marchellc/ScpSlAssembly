using System;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class PostProcessSubEffect : HypothermiaSubEffectBase
{
	[Serializable]
	private struct IntensityOverTemp
	{
		[SerializeField]
		private AnimationCurve _effectCurve;

		[SerializeField]
		private float _scpIntensityMultiplier;

		public readonly float GetValue(PostProcessSubEffect fx)
		{
			if (!fx._isAlive)
			{
				return 0f;
			}
			float num = this._effectCurve.Evaluate(fx._temperature);
			if (fx._isSCP)
			{
				num *= this._scpIntensityMultiplier;
			}
			return num;
		}
	}

	[SerializeField]
	private IntensityOverTemp _weightCurve;

	[SerializeField]
	private IntensityOverTemp _refCurve;

	[SerializeField]
	private IntensityOverTemp _intenCurve;

	[SerializeField]
	private IntensityOverTemp _frostCurve;

	private bool _isAlive;

	private bool _isSCP;

	private float _temperature;

	[SerializeField]
	private TemperatureSubEffect _temp;

	public override bool IsActive => this._temp.IsActive;

	internal override void UpdateEffect(float curExposure)
	{
		if (base.Hub.isLocalPlayer || base.MainEffect.IsSpectated)
		{
			this._isAlive = base.Hub.IsAlive();
			this._isSCP = base.Hub.IsSCP();
			this._temperature = this._temp.CurTemperature;
		}
	}

	public override void DisableEffect()
	{
		base.DisableEffect();
	}

	internal override void Init(StatusEffectBase mainEffect)
	{
		base.Init(mainEffect);
	}
}
