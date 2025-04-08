using System;
using UnityEngine;

public class RandomAmbienceTrigger : MonoBehaviour
{
	private void Start()
	{
		this.Rerandomize();
	}

	private void Update()
	{
		this._timeUntilAmbience -= Time.deltaTime;
		if (this._timeUntilAmbience > 0f)
		{
			return;
		}
		this._ambientSource.clip = this._ambientClips.RandomItem<AudioClip>();
		this._ambientSource.Play();
		this.Rerandomize();
	}

	private void Rerandomize()
	{
		this._timeUntilAmbience = global::UnityEngine.Random.Range(this._minAmbienceTime, this._maxAmbienceTime);
	}

	[SerializeField]
	private AudioClip[] _ambientClips;

	[SerializeField]
	private AudioSource _ambientSource;

	private float _timeUntilAmbience;

	[SerializeField]
	private float _minAmbienceTime;

	[SerializeField]
	private float _maxAmbienceTime;
}
