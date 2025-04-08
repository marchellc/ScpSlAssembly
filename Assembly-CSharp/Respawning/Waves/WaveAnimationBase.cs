using System;
using UnityEngine;

namespace Respawning.Waves
{
	public abstract class WaveAnimationBase<T> : MonoBehaviour where T : SpawnableWaveBase, IAnimatedWave
	{
		public T Wave { get; private set; }

		private protected Animator AnimatorTrigger { protected get; private set; }

		protected virtual void Awake()
		{
			T t;
			if (!WaveManager.TryGet<T>(out t))
			{
				throw new NullReferenceException("Could not find a reference to the T spawn wave.");
			}
			this.Wave = t;
			WaveManager.OnWaveTrigger += this.OnWaveTrigger;
		}

		protected virtual void OnWaveTriggered()
		{
			this.Wave.IsAnimationPlaying = true;
			this.AnimatorTrigger.SetTrigger(WaveAnimationBase<T>.PlayKey);
		}

		protected virtual void OnAnimationEnd()
		{
			this.Wave.IsAnimationPlaying = false;
		}

		protected virtual void Update()
		{
			bool isAnimationPlaying = this.Wave.IsAnimationPlaying;
			bool flag = Time.time >= this._animationTime;
			if (!isAnimationPlaying || !flag)
			{
				return;
			}
			this.OnAnimationEnd();
		}

		private void OnWaveTrigger(SpawnableWaveBase triggeredWave)
		{
			if (triggeredWave != this.Wave)
			{
				return;
			}
			this._animationTime = Time.time + this.Wave.AnimationDuration;
			this.OnWaveTriggered();
		}

		private void OnDestroy()
		{
			WaveManager.OnWaveTrigger -= this.OnWaveTrigger;
		}

		private static readonly int PlayKey = Animator.StringToHash("Play");

		private float _animationTime;
	}
}
