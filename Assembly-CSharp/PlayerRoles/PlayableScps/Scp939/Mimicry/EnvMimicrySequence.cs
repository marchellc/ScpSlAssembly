using System;
using System.Collections.Generic;
using AudioPooling;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class EnvMimicrySequence : ScriptableObject
	{
		public void EnqueueAll(int randomSeed)
		{
			global::UnityEngine.Random.State state = global::UnityEngine.Random.state;
			global::UnityEngine.Random.InitState(randomSeed);
			this._sounds.ForEach(new Action<EnvMimicrySequence.Sound>(this.EnqueueSound));
			global::UnityEngine.Random.state = state;
		}

		public bool UpdateSequence(Transform mimicPoint)
		{
			if (this._currentlyPlayed == null)
			{
				EnvMimicrySequence.Sound sound;
				if (!this._queuedSounds.TryDequeue(out sound))
				{
					return false;
				}
				AudioSourcePoolManager.PlayOnTransform(sound.Clips.RandomItem<AudioClip>(), mimicPoint, sound.Range, 1f, FalloffType.Exponential, sound.Channel, 1f);
				this._currentlyPlayed = sound;
			}
			this._currentlyPlayed.Duration -= Time.deltaTime;
			if (this._currentlyPlayed.Duration <= 0f)
			{
				this._currentlyPlayed = null;
			}
			return true;
		}

		private void EnqueueSound(EnvMimicrySequence.Sound s)
		{
			int num = global::UnityEngine.Random.Range(s.Repeat.x, s.Repeat.y + 1);
			while (num-- > 0)
			{
				EnvMimicrySequence.Sound sound = new EnvMimicrySequence.Sound
				{
					Clips = s.Clips,
					Channel = s.Channel,
					Duration = s.Duration,
					Range = s.Range
				};
				this._queuedSounds.Enqueue(sound);
			}
		}

		[SerializeField]
		private EnvMimicrySequence.Sound[] _sounds;

		private EnvMimicrySequence.Sound _currentlyPlayed;

		private readonly Queue<EnvMimicrySequence.Sound> _queuedSounds = new Queue<EnvMimicrySequence.Sound>();

		[Serializable]
		private class Sound
		{
			public float Range;

			public float Duration;

			public Vector2Int Repeat;

			public AudioClip[] Clips;

			public MixerChannel Channel;
		}
	}
}
