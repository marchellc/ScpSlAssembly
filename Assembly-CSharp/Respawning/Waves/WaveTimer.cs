using System;
using MapGeneration;
using Mirror;
using Respawning.Waves.Generic;
using UnityEngine;

namespace Respawning.Waves
{
	public class WaveTimer
	{
		public WaveTimer(TimeBasedWave wave)
		{
			this.DefaultSpawnInterval = wave.InitialSpawnInterval;
			this._wave = wave;
			StaticUnityMethods.OnUpdate += this.Update;
			SeedSynchronizer.OnGenerationFinished += this.OnMapGenerated;
			WaveManager.OnWaveUpdateMsgReceived += this.OnUpdateMessageReceived;
		}

		public float TimePassed { get; private set; }

		public float DefaultSpawnInterval { get; set; }

		public float SpawnIntervalSeconds { get; set; }

		public float TimeLeft
		{
			get
			{
				return this.SpawnIntervalSeconds - this.TimePassed;
			}
		}

		public bool IsReadyToSpawn
		{
			get
			{
				return this.TimePassed >= this.SpawnIntervalSeconds;
			}
		}

		public bool IsPaused
		{
			get
			{
				return this._pauseTimer > Time.time || this.IsOutOfRespawns;
			}
		}

		public float PauseTimeLeft
		{
			get
			{
				return Mathf.Max(this._pauseTimer - Time.time, 0f);
			}
		}

		private bool IsOutOfRespawns
		{
			get
			{
				ILimitedWave limitedWave = this._wave as ILimitedWave;
				return limitedWave != null && limitedWave.RespawnTokens <= 0;
			}
		}

		public void AddTime(float seconds)
		{
			this.SetTime(this.TimePassed + seconds);
		}

		public void SetTime(float seconds)
		{
			this.TimePassed = seconds;
			if (!NetworkServer.active)
			{
				return;
			}
			WaveUpdateMessage.ServerSendUpdate(this._wave, UpdateMessageFlags.Timer);
		}

		public void Reset(bool resetSpawnInterval = true)
		{
			this.TimePassed = 0f;
			if (resetSpawnInterval)
			{
				this.SpawnIntervalSeconds = this.DefaultSpawnInterval;
			}
			WaveUpdateMessage.ServerSendUpdate(this._wave, UpdateMessageFlags.Timer);
		}

		public void Pause(float duration)
		{
			this._pauseTimer = Time.time + duration;
			if (!NetworkServer.active)
			{
				return;
			}
			WaveUpdateMessage.ServerSendUpdate(this._wave, UpdateMessageFlags.Pause);
		}

		public void Destroy()
		{
			StaticUnityMethods.OnUpdate -= this.Update;
			SeedSynchronizer.OnGenerationFinished -= this.OnMapGenerated;
			WaveManager.OnWaveUpdateMsgReceived -= this.OnUpdateMessageReceived;
		}

		private void Update()
		{
			if (this.IsPaused && this.TimeLeft <= 30f)
			{
				return;
			}
			if (!RoundSummary.RoundInProgress())
			{
				return;
			}
			this.TimePassed += Time.deltaTime;
		}

		private void OnUpdateMessageReceived(WaveUpdateMessage msg)
		{
			if (this._wave != msg.Wave)
			{
				return;
			}
			if (msg.SpawnIntervalSeconds != null)
			{
				this.SpawnIntervalSeconds = msg.SpawnIntervalSeconds.Value;
			}
			if (msg.TimePassed != null)
			{
				this.TimePassed = msg.TimePassed.Value;
			}
			if (msg.PauseDuration != null)
			{
				this._pauseTimer = msg.PauseDuration.Value;
			}
		}

		private void OnMapGenerated()
		{
			this.Reset(true);
		}

		public const float CountdownPauseThresholdSeconds = 30f;

		private readonly TimeBasedWave _wave;

		private float _pauseTimer;
	}
}
