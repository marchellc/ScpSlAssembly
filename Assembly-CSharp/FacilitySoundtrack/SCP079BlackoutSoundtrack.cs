using System;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using UnityEngine;

namespace FacilitySoundtrack;

public class SCP079BlackoutSoundtrack : SoundtrackLayerBase
{
	private const float BlackoutTime = 60f;

	private const float FadeInTime = 10f;

	private const float PlayDelay = 10.25f;

	private const float FadeoutTime = 1f;

	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private float _fadeInMultipler;

	[SerializeField]
	private float _fadeOutMultipler;

	private float _weight;

	private FacilityZone _blackoutZone;

	private double _blackoutTime;

	public override float Weight => this._weight;

	public override bool Additive => false;

	public override void UpdateVolume(float volumeScale)
	{
		if (ReferenceHub.TryGetPovHub(out var hub))
		{
			this._audioSource.volume = volumeScale * this._weight;
			bool num = hub.IsSCP();
			bool flag = this._blackoutTime + 60.0 - 1.0 < NetworkTime.time;
			bool flag2 = this._blackoutTime + 10.0 > NetworkTime.time;
			if (num || flag || flag2)
			{
				this.Fade(increase: false);
			}
			else if (hub.GetCurrentZone() != this._blackoutZone)
			{
				this.Fade(increase: false);
			}
			else
			{
				this.Fade(increase: true);
			}
		}
	}

	private void Fade(bool increase)
	{
		this._weight = Mathf.Lerp(this._weight, increase ? 1 : 0, (increase ? this._fadeInMultipler : this._fadeOutMultipler) * Time.deltaTime);
	}

	private void OnClientZoneBlackout(ReferenceHub scp079Hub, FacilityZone zone)
	{
		this._audioSource.PlayDelayed(10.25f);
		this._blackoutZone = zone;
		this._blackoutTime = NetworkTime.time;
	}

	private void Start()
	{
		Scp079BlackoutZoneAbility.OnClientZoneBlackout = (Action<ReferenceHub, FacilityZone>)Delegate.Combine(Scp079BlackoutZoneAbility.OnClientZoneBlackout, new Action<ReferenceHub, FacilityZone>(OnClientZoneBlackout));
	}

	private void OnDestroy()
	{
		Scp079BlackoutZoneAbility.OnClientZoneBlackout = (Action<ReferenceHub, FacilityZone>)Delegate.Remove(Scp079BlackoutZoneAbility.OnClientZoneBlackout, new Action<ReferenceHub, FacilityZone>(OnClientZoneBlackout));
	}
}
