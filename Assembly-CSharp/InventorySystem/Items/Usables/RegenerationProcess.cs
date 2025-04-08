using System;
using UnityEngine;

namespace InventorySystem.Items.Usables
{
	public class RegenerationProcess
	{
		public RegenerationProcess(AnimationCurve regenCurve, float speedMultiplier, float healthPointsMultiplier)
		{
			this._regenCurve = regenCurve;
			this._maxTime = regenCurve.keys[regenCurve.length - 1].time;
			this._speedMultip = speedMultiplier;
			this._hpMultip = healthPointsMultiplier * this._speedMultip;
			this._healValue = 0f;
			this._elapsed = 0f;
		}

		public void GetValue(out bool isDone, out int value)
		{
			this._elapsed += Time.deltaTime * this._speedMultip;
			this._healValue += this._regenCurve.Evaluate(this._elapsed) * Time.deltaTime * this._hpMultip;
			value = (int)this._healValue;
			this._healValue -= (float)value;
			isDone = this._elapsed >= this._maxTime;
		}

		private readonly AnimationCurve _regenCurve;

		private readonly float _maxTime;

		private readonly float _speedMultip;

		private readonly float _hpMultip;

		private float _healValue;

		private float _elapsed;
	}
}
