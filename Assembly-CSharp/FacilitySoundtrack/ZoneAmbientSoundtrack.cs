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

	public override float Weight => _weight;

	public override void UpdateVolume(float masterScale)
	{
		if (MainCameraController.LastPosition.TryGetRoom(out var room))
		{
			_lastZone = room.Zone;
		}
		float num = (base.IsPovMuted ? (0f - _fadeOutSpeed) : _fadeInSpeed);
		_weight = Mathf.Clamp01(_weight + num * Time.deltaTime);
		ZoneSoundtrack[] zoneSoundtracks = _zoneSoundtracks;
		foreach (ZoneSoundtrack zoneSoundtrack in zoneSoundtracks)
		{
			float target = ((zoneSoundtrack.TargetZone == _lastZone) ? 1 : 0);
			zoneSoundtrack.CrossfadeVolume = Mathf.MoveTowards(zoneSoundtrack.CrossfadeVolume, target, _crossfadeSpeed * Time.deltaTime);
			zoneSoundtrack.Source.volume = zoneSoundtrack.CrossfadeVolume * zoneSoundtrack.VolumeScale * masterScale;
		}
	}
}
