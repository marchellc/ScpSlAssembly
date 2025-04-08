using System;
using System.Diagnostics;
using UnityEngine;

namespace CameraShaking
{
	public class RecoilShake : IShakeEffect
	{
		public RecoilShake(RecoilSettings settings)
		{
			this._settings = settings;
			this._startQuaternion = Quaternion.Euler(0f, 0f, settings.ZAxis * (global::UnityEngine.Random.value - 0.5f));
			this._firstFrame = true;
			this._removeStopwatch = new Stopwatch();
			this._removeStopwatch.Start();
		}

		public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
		{
			float num = Mathf.Clamp01((float)this._removeStopwatch.Elapsed.TotalSeconds / this._settings.AnimationTime);
			float num2;
			float num3;
			if (this._firstFrame)
			{
				num2 = this._settings.UpKick;
				num3 = this._settings.SideKick;
				this._firstFrame = false;
			}
			else
			{
				num2 = 0f;
				num3 = 0f;
			}
			float num4 = num2;
			float num5 = num3;
			Quaternion? quaternion = new Quaternion?(Quaternion.Slerp(this._startQuaternion, Quaternion.identity, num));
			float num6 = Mathf.SmoothStep(this._settings.FovKick, 1f, num);
			shakeValues = new ShakeEffectValues(quaternion, null, null, num6, num4, num5);
			return num < 1f;
		}

		private readonly Stopwatch _removeStopwatch;

		private readonly RecoilSettings _settings;

		private readonly Quaternion _startQuaternion;

		private bool _firstFrame;
	}
}
