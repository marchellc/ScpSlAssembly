using UnityEngine;

public class RandomAmbienceTrigger : MonoBehaviour
{
	[SerializeField]
	private AudioClip[] _ambientClips;

	[SerializeField]
	private AudioSource _ambientSource;

	private float _timeUntilAmbience;

	[SerializeField]
	private float _minAmbienceTime;

	[SerializeField]
	private float _maxAmbienceTime;

	private void Start()
	{
		this.Rerandomize();
	}

	private void Update()
	{
		this._timeUntilAmbience -= Time.deltaTime;
		if (!(this._timeUntilAmbience > 0f))
		{
			this._ambientSource.clip = this._ambientClips.RandomItem();
			this._ambientSource.Play();
			this.Rerandomize();
		}
	}

	private void Rerandomize()
	{
		this._timeUntilAmbience = Random.Range(this._minAmbienceTime, this._maxAmbienceTime);
	}
}
