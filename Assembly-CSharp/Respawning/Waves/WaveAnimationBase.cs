using System;
using UnityEngine;

namespace Respawning.Waves;

public abstract class WaveAnimationBase<T> : MonoBehaviour where T : SpawnableWaveBase, IAnimatedWave
{
	private static readonly int PlayKey = Animator.StringToHash("Play");

	private float _animationTime;

	public T Wave { get; private set; }

	protected Animator AnimatorTrigger { get; private set; }

	protected virtual void Awake()
	{
		if (!WaveManager.TryGet<T>(out var spawnWave))
		{
			throw new NullReferenceException("Could not find a reference to the T spawn wave.");
		}
		this.Wave = spawnWave;
		WaveManager.OnWaveTrigger += OnWaveTrigger;
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
		if (isAnimationPlaying && flag)
		{
			this.OnAnimationEnd();
		}
	}

	private void OnWaveTrigger(SpawnableWaveBase triggeredWave)
	{
		if (triggeredWave == this.Wave)
		{
			this._animationTime = Time.time + this.Wave.AnimationDuration;
			this.OnWaveTriggered();
		}
	}

	private void OnDestroy()
	{
		WaveManager.OnWaveTrigger -= OnWaveTrigger;
	}
}
