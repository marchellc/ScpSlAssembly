using System;
using Respawning.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace Respawning.Graphics
{
	public class WaveTimerDisplay : SerializedWaveInterface
	{
		private float NormalizedTimerValue
		{
			get
			{
				float num = Mathf.Max(this._timeBasedWave.Timer.SpawnIntervalSeconds, 1f);
				float num2 = (Mathf.Clamp(this._timeBasedWave.Timer.TimePassed, 0f, num) - 0f) / (num - 0f);
				return 1f - num2;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			TimeBasedWave timeBasedWave = base.Wave as TimeBasedWave;
			if (timeBasedWave == null)
			{
				throw new NullReferenceException("Unable to convert " + base.Wave.GetType().Name + " to TimeBasedWave.");
			}
			this._timeBasedWave = timeBasedWave;
			this._isTimeBased = true;
		}

		private void Update()
		{
			if (!this._isTimeBased)
			{
				return;
			}
			this._slider.value = this.NormalizedTimerValue;
		}

		[SerializeField]
		private Slider _slider;

		private bool _isTimeBased;

		private TimeBasedWave _timeBasedWave;
	}
}
