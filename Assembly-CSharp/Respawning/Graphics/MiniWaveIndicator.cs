using GameCore;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Respawning.Graphics;

public abstract class MiniWaveIndicator<TMiniWave> : WaveInterfaceBase<TMiniWave> where TMiniWave : TimeBasedWave, ILimitedWave, IMiniWave
{
	private const float NormalCountdownThreshold = 8f;

	private const float FastCountdownThreshold = 3f;

	public AnimationCurve NormalAnimation;

	public AnimationCurve FastAnimation;

	private Image _miniWaveIndicator;

	protected override void Awake()
	{
		base.Awake();
		this._miniWaveIndicator = base.GetComponent<Image>();
	}

	private void Update()
	{
		Color color = this._miniWaveIndicator.color;
		color.a = this.GetAlphaColor();
		this._miniWaveIndicator.color = color;
	}

	private float GetAlphaColor()
	{
		if (base.Wave.RespawnTokens <= 0 || !base.Wave.IsReadyToSpawn)
		{
			return 0f;
		}
		float timeLeft = base.Wave.Timer.TimeLeft;
		if (timeLeft > 8f)
		{
			return 0f;
		}
		float time = (float)RoundStart.RoundLength.TotalSeconds;
		if (!(timeLeft <= 3f))
		{
			return this.NormalAnimation.Evaluate(time);
		}
		return this.FastAnimation.Evaluate(time);
	}
}
