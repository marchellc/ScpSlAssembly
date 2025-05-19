using System;
using System.Diagnostics;
using AudioPooling;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson;

[Serializable]
public class ModelShotSettings
{
	[SerializeField]
	private AudioClip[] _onShotSoundClips;

	[SerializeField]
	private MixerChannel _onShotSoundChannel;

	[SerializeField]
	private float _onShotSoundRange;

	private const float MinCooldown = 0.06f;

	private readonly Stopwatch _lastShot = Stopwatch.StartNew();

	public void PlayOnShotSound(HitboxIdentity hitHitbox, bool firstperson)
	{
		if (_onShotSoundClips.Length != 0 && !(_lastShot.Elapsed.TotalSeconds < 0.05999999865889549))
		{
			if (firstperson)
			{
				AudioSourcePoolManager.Play2D(_onShotSoundClips.RandomItem(), 1f, _onShotSoundChannel);
			}
			else
			{
				AudioSourcePoolManager.PlayOnTransform(_onShotSoundClips.RandomItem(), hitHitbox.transform, _onShotSoundRange, 1f, FalloffType.Exponential, _onShotSoundChannel);
			}
			_lastShot.Restart();
		}
	}
}
