using System;
using System.Collections.Generic;
using Respawning.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace Respawning.Graphics
{
	public class PauseDisplayColorChanger : SerializedWaveInterface
	{
		private bool IsWavePaused
		{
			get
			{
				return this._cachedState;
			}
			set
			{
				if (value == this._cachedState)
				{
					return;
				}
				this._cachedState = value;
				if (!value)
				{
					this.RevertGraphicColors();
					return;
				}
				this.ModifyGraphicColors();
			}
		}

		protected override void Awake()
		{
			base.Awake();
			TimeBasedWave timeBasedWave = base.Wave as TimeBasedWave;
			if (timeBasedWave == null)
			{
				throw new NullReferenceException("Unable to convert " + base.Wave.GetType().Name + " to TimeBasedWave.");
			}
			this._timeBasedWave = timeBasedWave;
			this._isTimeBased = true;
		}

		private void Update()
		{
			if (!this._isTimeBased)
			{
				return;
			}
			this.IsWavePaused = this._timeBasedWave.Timer.IsPaused;
		}

		private void ModifyGraphicColors()
		{
			foreach (Graphic graphic in this.TargetGraphics)
			{
				Color color = graphic.color;
				this._colorCache[graphic] = color;
				graphic.color = this.PausedColor;
			}
		}

		private void RevertGraphicColors()
		{
			foreach (Graphic graphic in this.TargetGraphics)
			{
				Color color;
				if (this._colorCache.TryGetValue(graphic, out color))
				{
					graphic.color = color;
				}
			}
			this._colorCache.Clear();
		}

		public Graphic[] TargetGraphics;

		public Color PausedColor;

		private readonly Dictionary<Graphic, Color> _colorCache = new Dictionary<Graphic, Color>();

		private TimeBasedWave _timeBasedWave;

		private bool _isTimeBased;

		private bool _cachedState;
	}
}
