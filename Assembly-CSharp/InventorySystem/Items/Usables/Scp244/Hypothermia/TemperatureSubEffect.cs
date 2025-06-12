using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class TemperatureSubEffect : HypothermiaSubEffectBase
{
	[SerializeField]
	private float _maxExitTemp;

	[SerializeField]
	private float _temperatureDrop;

	public float CurTemperature { get; private set; }

	public override bool IsActive => this.CurTemperature > 0f;

	internal override void UpdateEffect(float curExposure)
	{
		if (!base.Hub.IsAlive())
		{
			this.DisableEffect();
		}
		else if (curExposure == 0f)
		{
			if (this.CurTemperature != 0f)
			{
				float value = this.CurTemperature - this._temperatureDrop * Time.deltaTime;
				this.CurTemperature = Mathf.Clamp(value, 0f, this._maxExitTemp);
			}
		}
		else
		{
			this.CurTemperature += curExposure * RainbowTaste.CurrentMultiplier(base.Hub) * Time.deltaTime;
		}
	}

	public override void DisableEffect()
	{
		this.CurTemperature = 0f;
	}
}
