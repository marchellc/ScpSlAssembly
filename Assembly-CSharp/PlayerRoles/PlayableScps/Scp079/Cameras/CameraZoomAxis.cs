using System;
using System.Diagnostics;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Cameras;

[Serializable]
public class CameraZoomAxis : CameraAxisBase
{
	private const string ScrollAxis = "Mouse ScrollWheel";

	private readonly Stopwatch _cooldownStopwatch = Stopwatch.StartNew();

	private float _lastSoundZoom;

	private Offset _unzoomedOffset;

	[SerializeField]
	private Transform _zoomBone;

	[SerializeField]
	private Offset _zoomedOffset;

	[SerializeField]
	private AnimationCurve _magnificationCurve;

	[SerializeField]
	private float _stepSize;

	[SerializeField]
	private float _cooldown;

	public float CurrentZoom => this._magnificationCurve.Evaluate(base.CurValue);

	internal override void Update(Scp079Camera cam)
	{
		if (Scp079Role.LocalInstanceActive && cam != null && cam.IsUsedByLocalPlayer && cam.IsActive)
		{
			this.UpdateInputs();
		}
		base.Update(cam);
	}

	internal override void Awake(Scp079Camera cam)
	{
		base.Awake(cam);
		this._unzoomedOffset = new Offset
		{
			position = this._zoomBone.localPosition,
			rotation = this._zoomBone.localEulerAngles,
			scale = this._zoomBone.localScale
		};
		base.TargetValue = 0f;
	}

	protected override void OnValueChanged(float newValue, Scp079Camera cam)
	{
		float t = Mathf.InverseLerp(base.MinValue, base.MaxValue, newValue);
		this._zoomBone.localPosition = Vector3.Lerp(this._unzoomedOffset.position, this._zoomedOffset.position, t);
		this._zoomBone.localRotation = Quaternion.Lerp(Quaternion.Euler(this._unzoomedOffset.rotation), Quaternion.Euler(this._zoomedOffset.rotation), t);
		this._zoomBone.localScale = Vector3.Lerp(this._unzoomedOffset.scale, this._zoomedOffset.scale, t);
		if (!base.SoundEffectSource.loop && this._lastSoundZoom != base.TargetValue)
		{
			base.SoundEffectSource.Play();
			this._lastSoundZoom = base.TargetValue;
		}
	}

	private void UpdateInputs()
	{
		if (!Scp079CursorManager.LockCameras)
		{
			float axisRaw = Input.GetAxisRaw("Mouse ScrollWheel");
			if (axisRaw != 0f && this._cooldownStopwatch.Elapsed.TotalSeconds >= (double)this._cooldown)
			{
				base.TargetValue += ((axisRaw > 0f) ? this._stepSize : (0f - this._stepSize));
				this._cooldownStopwatch.Restart();
			}
		}
	}
}
