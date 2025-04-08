using System;
using AudioPooling;
using Decals;
using UnityEngine;

public class BulletHoleDecal : Decal
{
	public override void AttachToSurface(RaycastHit hitSurface)
	{
		base.AttachToSurface(hitSurface);
		ParticleSystem fromPool = this._impactParticlesTemplate.GetFromPool(0.1f);
		fromPool.Play(true);
		Transform transform = fromPool.transform;
		transform.SetParent(this._impactParticlesTargetPosition);
		transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		AudioSourcePoolManager.PlayOnTransform(((base.CachedTransform.parent.gameObject.layer == 27) ? this._doorImpactSounds : this._wallImpactSounds).RandomItem<AudioClip>(), base.CachedTransform, this._impactSoundRange, this._impactSoundVolume, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f).Source.pitch += global::UnityEngine.Random.Range(-this._impactSoundRandomization, this._impactSoundRandomization);
	}

	[SerializeField]
	private ParticleSystem _impactParticlesTemplate;

	[SerializeField]
	private Transform _impactParticlesTargetPosition;

	[SerializeField]
	private AudioClip[] _wallImpactSounds;

	[SerializeField]
	private AudioClip[] _doorImpactSounds;

	[SerializeField]
	private float _impactSoundRange = 10f;

	[SerializeField]
	private float _impactSoundVolume = 1f;

	[SerializeField]
	private float _impactSoundRandomization;

	private const int DoorLayer = 27;
}
