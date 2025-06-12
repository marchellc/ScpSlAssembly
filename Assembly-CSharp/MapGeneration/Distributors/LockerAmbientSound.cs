using System.Diagnostics;
using UnityEngine;

namespace MapGeneration.Distributors;

public class LockerAmbientSound : MonoBehaviour
{
	[SerializeField]
	private AudioSource _closedAmbientSource;

	[SerializeField]
	private AudioSource _openAmbinetSource;

	[SerializeField]
	private AnimationCurve _crossfadeOverTime;

	private LockerChamber _chamber;

	private float _crossfadeDuration;

	private readonly Stopwatch _animSw = Stopwatch.StartNew();

	private void Awake()
	{
		this._chamber = base.GetComponent<LockerChamber>();
		this._chamber.OnDoorStatusSet += UpdateStatus;
		this._crossfadeDuration = this._crossfadeOverTime.GetDuration();
		this.UpdateStatus();
	}

	private void UpdateStatus()
	{
		base.enabled = true;
		this._animSw.Restart();
	}

	private void Update()
	{
		float num = (float)this._animSw.Elapsed.TotalSeconds;
		float time = (this._chamber.IsOpen ? num : (this._crossfadeDuration - num));
		float num2 = Mathf.Clamp01(this._crossfadeOverTime.Evaluate(time));
		this._openAmbinetSource.volume = num2;
		this._closedAmbientSource.volume = 1f - num2;
		if (num > this._crossfadeDuration)
		{
			base.enabled = false;
		}
	}
}
