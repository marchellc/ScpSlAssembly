using System;
using System.Diagnostics;
using AudioPooling;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson
{
	[Serializable]
	public class ModelShotSettings
	{
		public void PlayOnShotSound(HitboxIdentity hitHitbox, bool firstperson)
		{
			if (this._onShotSoundClips.Length == 0)
			{
				return;
			}
			if (this._lastShot.Elapsed.TotalSeconds < 0.05999999865889549)
			{
				return;
			}
			if (firstperson)
			{
				AudioSourcePoolManager.Play2D(this._onShotSoundClips.RandomItem<AudioClip>(), 1f, this._onShotSoundChannel, 1f);
			}
			else
			{
				AudioSourcePoolManager.PlayOnTransform(this._onShotSoundClips.RandomItem<AudioClip>(), hitHitbox.transform, this._onShotSoundRange, 1f, FalloffType.Exponential, this._onShotSoundChannel, 1f);
			}
			this._lastShot.Restart();
		}

		[SerializeField]
		private AudioClip[] _onShotSoundClips;

		[SerializeField]
		private MixerChannel _onShotSoundChannel;

		[SerializeField]
		private float _onShotSoundRange;

		private const float MinCooldown = 0.06f;

		private readonly Stopwatch _lastShot = Stopwatch.StartNew();
	}
}
