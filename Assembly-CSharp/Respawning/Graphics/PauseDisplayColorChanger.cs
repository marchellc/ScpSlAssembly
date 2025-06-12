using System;
using Respawning.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace Respawning.Graphics;

public class PauseDisplayColorChanger : SerializedWaveInterface
{
	public Graphic[] TargetGraphics;

	public Color DefaultColor;

	public Color PausedColor;

	public AnimationCurve BlinkingAnimation;

	private TimeBasedWave _timeBasedWave;

	private bool _isTimeBased;

	private bool _blinkTimer;

	protected override void Awake()
	{
		base.Awake();
		if (!(base.Wave is TimeBasedWave timeBasedWave))
		{
			throw new NullReferenceException("Unable to convert " + base.Wave.GetType().Name + " to TimeBasedWave.");
		}
		this._timeBasedWave = timeBasedWave;
		this._isTimeBased = true;
		WaveManager.OnWaveUpdateMsgReceived += OnWaveUpdateMsgReceived;
	}

	private void OnWaveUpdateMsgReceived(WaveUpdateMessage msg)
	{
		if (this._timeBasedWave == msg.Wave)
		{
			if (msg.IsTrigger)
			{
				this._blinkTimer = true;
			}
			else if (msg.IsSpawn)
			{
				this._blinkTimer = false;
			}
		}
	}

	private void Update()
	{
		if (this._isTimeBased)
		{
			if (this._blinkTimer)
			{
				float t = this.BlinkingAnimation.Evaluate(Time.time);
				Color color = Color.Lerp(this.PausedColor, this.DefaultColor, t);
				this.ModifyGraphicColors(color);
			}
			else if (this._timeBasedWave.Timer.IsPaused)
			{
				this.ModifyGraphicColors(this.PausedColor);
			}
			else
			{
				this.ModifyGraphicColors(this.DefaultColor);
			}
		}
	}

	private void ModifyGraphicColors(Color color)
	{
		Graphic[] targetGraphics = this.TargetGraphics;
		for (int i = 0; i < targetGraphics.Length; i++)
		{
			targetGraphics[i].color = color;
		}
	}
}
