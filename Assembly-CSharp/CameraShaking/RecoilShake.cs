using System.Diagnostics;
using UnityEngine;

namespace CameraShaking;

public class RecoilShake : IShakeEffect
{
	private readonly Stopwatch _removeStopwatch;

	private readonly RecoilSettings _settings;

	private readonly Quaternion _startQuaternion;

	private bool _firstFrame;

	public RecoilShake(RecoilSettings settings)
	{
		this._settings = settings;
		this._startQuaternion = Quaternion.Euler(0f, 0f, settings.ZAxis * (Random.value - 0.5f));
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
		float verticalLook = num2;
		float horizontalLook = num3;
		Quaternion? rootCameraRotation = Quaternion.Slerp(this._startQuaternion, Quaternion.identity, num);
		float fovPercent = Mathf.SmoothStep(this._settings.FovKick, 1f, num);
		shakeValues = new ShakeEffectValues(rootCameraRotation, null, null, fovPercent, verticalLook, horizontalLook);
		return num < 1f;
	}
}
