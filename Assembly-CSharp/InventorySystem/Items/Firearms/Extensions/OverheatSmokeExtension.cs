using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class OverheatSmokeExtension : OverheatExtensionBase
{
	[SerializeField]
	private float _temperatureThreshold;

	[SerializeField]
	private ParticleSystem _particleSystem;

	private bool _lastExceeded;

	public override void SetupAny()
	{
		base.SetupAny();
		this.UpdateParticleSystem(TemperatureTrackerModule.GetTemperature(base.Identifier), forceUpdate: true);
	}

	protected override void OnTemperatureChanged(float temp)
	{
		this.UpdateParticleSystem(temp, forceUpdate: false);
	}

	private void UpdateParticleSystem(float curTemp, bool forceUpdate)
	{
		bool flag = curTemp > this._temperatureThreshold;
		if (this._lastExceeded != flag || forceUpdate)
		{
			ParticleSystem.EmissionModule emission = this._particleSystem.emission;
			emission.enabled = flag;
			this._lastExceeded = flag;
		}
	}
}
