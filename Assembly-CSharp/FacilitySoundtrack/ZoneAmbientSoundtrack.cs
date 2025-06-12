using System;
using MapGeneration;
using UnityEngine;

namespace FacilitySoundtrack;

public class ZoneAmbientSoundtrack : SoundtrackLayerBase
{
	[Serializable]
	private class ZoneSoundtrack
	{
		public FacilityZone TargetZone;

		public AudioSource Source;

		public float VolumeScale;

		public float CrossfadeVolume { get; set; }
	}

	[SerializeField]
	private float _fadeInSpeed;

	[SerializeField]
	private float _fadeOutSpeed;

	[SerializeField]
	private float _crossfadeSpeed;

	[SerializeField]
	private ZoneSoundtrack[] _zoneSoundtracks;

	private float _weight;

	private FacilityZone _lastZone;

	public override bool Additive => true;

	public override float Weight => this._weight;

	public override void UpdateVolume(float masterScale)
	{
		if (MainCameraController.LastPosition.TryGetRoom(out var room))
		{
			this._lastZone = room.Zone;
		}
		float num = (base.IsPovMuted ? (0f - this._fadeOutSpeed) : this._fadeInSpeed);
		this._weight = Mathf.Clamp01(this._weight + num * Time.deltaTime);
		ZoneSoundtrack[] zoneSoundtracks = this._zoneSoundtracks;
		foreach (ZoneSoundtrack zoneSoundtrack in zoneSoundtracks)
		{
			float target = ((zoneSoundtrack.TargetZone == this._lastZone) ? 1 : 0);
			zoneSoundtrack.CrossfadeVolume = Mathf.MoveTowards(zoneSoundtrack.CrossfadeVolume, target, this._crossfadeSpeed * Time.deltaTime);
			zoneSoundtrack.Source.volume = zoneSoundtrack.CrossfadeVolume * zoneSoundtrack.VolumeScale * masterScale;
		}
	}
}
