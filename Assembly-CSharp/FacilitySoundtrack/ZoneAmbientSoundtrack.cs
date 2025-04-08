using System;
using MapGeneration;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class ZoneAmbientSoundtrack : SoundtrackLayerBase
	{
		public override bool Additive
		{
			get
			{
				return true;
			}
		}

		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public override void UpdateVolume(float masterScale)
		{
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(MainCameraController.CurrentCamera.position);
			if (roomIdentifier != null)
			{
				this._lastZone = roomIdentifier.Zone;
			}
			float num = (base.IsPovMuted ? (-this._fadeOutSpeed) : this._fadeInSpeed);
			this._weight = Mathf.Clamp01(this._weight + num * Time.deltaTime);
			foreach (ZoneAmbientSoundtrack.ZoneSoundtrack zoneSoundtrack in this._zoneSoundtracks)
			{
				float num2 = (float)((zoneSoundtrack.TargetZone == this._lastZone) ? 1 : 0);
				zoneSoundtrack.CrossfadeVolume = Mathf.MoveTowards(zoneSoundtrack.CrossfadeVolume, num2, this._crossfadeSpeed * Time.deltaTime);
				zoneSoundtrack.Source.volume = zoneSoundtrack.CrossfadeVolume * zoneSoundtrack.VolumeScale * masterScale;
			}
		}

		[SerializeField]
		private float _fadeInSpeed;

		[SerializeField]
		private float _fadeOutSpeed;

		[SerializeField]
		private float _crossfadeSpeed;

		[SerializeField]
		private ZoneAmbientSoundtrack.ZoneSoundtrack[] _zoneSoundtracks;

		private float _weight;

		private FacilityZone _lastZone;

		[Serializable]
		private class ZoneSoundtrack
		{
			public float CrossfadeVolume { get; set; }

			public FacilityZone TargetZone;

			public AudioSource Source;

			public float VolumeScale;
		}
	}
}
