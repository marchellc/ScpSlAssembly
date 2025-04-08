using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class OverheatSmokeExtension : OverheatExtensionBase
	{
		public override void SetupAny()
		{
			base.SetupAny();
			this.UpdateParticleSystem(TemperatureTrackerModule.GetTemperature(base.Identifier), true);
		}

		protected override void OnTemperatureChanged(float temp)
		{
			this.UpdateParticleSystem(temp, false);
		}

		private void UpdateParticleSystem(float curTemp, bool forceUpdate)
		{
			bool flag = curTemp > this._temperatureThreshold;
			if (this._lastExceeded == flag && !forceUpdate)
			{
				return;
			}
			this._particleSystem.emission.enabled = flag;
			this._lastExceeded = flag;
		}

		[SerializeField]
		private float _temperatureThreshold;

		[SerializeField]
		private ParticleSystem _particleSystem;

		private bool _lastExceeded;
	}
}
