using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioReverbFilter))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioProximityEffects : MonoBehaviour
{
	private float ProximityLevel
	{
		get
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return 0f;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return 0f;
			}
			return Vector3.Distance(base.transform.position, fpcRole.FpcModule.Position) / this._audioSource.maxDistance * this._audioSource.spatialBlend;
		}
	}

	private void Awake()
	{
		this._audioSource = base.GetComponent<AudioSource>();
		this._reverbFilter = base.GetComponent<AudioReverbFilter>();
		this._lowPassFilter = base.GetComponent<AudioLowPassFilter>();
	}

	private void Update()
	{
		float proximityLevel = this.ProximityLevel;
		this._reverbFilter.dryLevel = this._reverbDryOverDistance.Evaluate(proximityLevel);
		this._reverbFilter.room = this._reverbSizeOverDistance.Evaluate(proximityLevel);
		this._lowPassFilter.cutoffFrequency = this._lowpassOverDistance.Evaluate(proximityLevel);
	}

	[SerializeField]
	private AnimationCurve _reverbSizeOverDistance;

	[SerializeField]
	private AnimationCurve _reverbDryOverDistance;

	[SerializeField]
	private AnimationCurve _lowpassOverDistance;

	private AudioSource _audioSource;

	private AudioReverbFilter _reverbFilter;

	private AudioLowPassFilter _lowPassFilter;
}
