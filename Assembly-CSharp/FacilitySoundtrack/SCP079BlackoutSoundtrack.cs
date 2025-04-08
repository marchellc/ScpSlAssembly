using System;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class SCP079BlackoutSoundtrack : SoundtrackLayerBase
	{
		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public override bool Additive
		{
			get
			{
				return false;
			}
		}

		public override void UpdateVolume(float volumeScale)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetPovHub(out referenceHub))
			{
				return;
			}
			this._audioSource.volume = volumeScale * this._weight;
			bool flag = referenceHub.IsSCP(true);
			bool flag2 = this._blackoutTime + 60.0 - 1.0 < NetworkTime.time;
			if (flag || flag2)
			{
				this.Fade(false);
				return;
			}
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(MainCameraController.CurrentCamera.position);
			if (roomIdentifier == null || roomIdentifier.Zone != this._blackoutZone || this._blackoutTime + 10.0 > NetworkTime.time)
			{
				this.Fade(false);
				return;
			}
			this.Fade(true);
		}

		private void Fade(bool increase)
		{
			this._weight = Mathf.Lerp(this._weight, (float)(increase ? 1 : 0), (increase ? this._fadeInMultipler : this._fadeOutMultipler) * Time.deltaTime);
		}

		private void OnClientZoneBlackout(ReferenceHub scp079Hub, FacilityZone zone)
		{
			this._audioSource.PlayDelayed(10.25f);
			this._blackoutZone = zone;
			this._blackoutTime = NetworkTime.time;
		}

		private void Start()
		{
			Scp079BlackoutZoneAbility.OnClientZoneBlackout = (Action<ReferenceHub, FacilityZone>)Delegate.Combine(Scp079BlackoutZoneAbility.OnClientZoneBlackout, new Action<ReferenceHub, FacilityZone>(this.OnClientZoneBlackout));
		}

		private void OnDestroy()
		{
			Scp079BlackoutZoneAbility.OnClientZoneBlackout = (Action<ReferenceHub, FacilityZone>)Delegate.Remove(Scp079BlackoutZoneAbility.OnClientZoneBlackout, new Action<ReferenceHub, FacilityZone>(this.OnClientZoneBlackout));
		}

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
	}
}
