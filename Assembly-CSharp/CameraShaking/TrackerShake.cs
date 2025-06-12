using UnityEngine;

namespace CameraShaking;

public class TrackerShake : IShakeEffect
{
	private const float FadeoutTime = 0.1f;

	private readonly float _intensity;

	private readonly Transform _tracker;

	private readonly Quaternion _offset;

	private bool _isVisible;

	private float _remainingFade;

	private Quaternion _lastKnownRotation;

	public TrackerShake(Transform target, Vector3 offset, float intensity = 1f)
		: this(target, Quaternion.Euler(offset), intensity)
	{
	}

	public TrackerShake(Transform target, Quaternion offset, float intensity = 1f)
	{
		this._tracker = target;
		this._offset = offset;
		this._intensity = intensity;
		this._remainingFade = 0.1f;
		this._isVisible = true;
	}

	public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
	{
		if (this._tracker == null || !this._tracker.gameObject.activeInHierarchy)
		{
			this._isVisible = false;
		}
		Quaternion quaternion;
		if (this._isVisible)
		{
			quaternion = this._tracker.localRotation * this._offset;
			if (this._intensity != 1f)
			{
				quaternion = Quaternion.LerpUnclamped(Quaternion.identity, quaternion, this._intensity);
			}
			this._lastKnownRotation = quaternion;
		}
		else
		{
			this._remainingFade -= Time.deltaTime;
			quaternion = Quaternion.Lerp(Quaternion.identity, this._lastKnownRotation, this._remainingFade / 0.1f);
		}
		Vector3 eulerAngles = quaternion.eulerAngles;
		quaternion = Quaternion.Euler(0f - eulerAngles.x, eulerAngles.z, eulerAngles.y);
		shakeValues = new ShakeEffectValues(quaternion, Quaternion.Inverse(quaternion));
		return this._remainingFade > 0f;
	}
}
