using System;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class TemperatureSubEffect : HypothermiaSubEffectBase
	{
		public float CurTemperature { get; private set; }

		public override bool IsActive
		{
			get
			{
				return this.CurTemperature > 0f;
			}
		}

		internal override void UpdateEffect(float curExposure)
		{
			if (!base.Hub.IsAlive())
			{
				this.DisableEffect();
				return;
			}
			if (curExposure != 0f)
			{
				this.CurTemperature += curExposure * RainbowTaste.CurrentMultiplier(base.Hub) * Time.deltaTime;
				return;
			}
			if (this.CurTemperature == 0f)
			{
				return;
			}
			float num = this.CurTemperature - this._temperatureDrop * Time.deltaTime;
			this.CurTemperature = Mathf.Clamp(num, 0f, this._maxExitTemp);
		}

		public override void DisableEffect()
		{
			this.CurTemperature = 0f;
		}

		[SerializeField]
		private float _maxExitTemp;

		[SerializeField]
		private float _temperatureDrop;
	}
}
