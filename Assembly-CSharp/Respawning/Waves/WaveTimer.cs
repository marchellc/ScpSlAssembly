using MapGeneration;
using Mirror;
using Respawning.Waves.Generic;
using UnityEngine;

namespace Respawning.Waves;

public class WaveTimer
{
	public const float CountdownPauseThresholdSeconds = 30f;

	private readonly TimeBasedWave _wave;

	private float _pauseTimer;

	public float TimePassed { get; private set; }

	public float DefaultSpawnInterval { get; set; }

	public float SpawnIntervalSeconds { get; set; }

	public float TimeLeft => this.SpawnIntervalSeconds - this.TimePassed;

	public bool IsReadyToSpawn => this.TimePassed >= this.SpawnIntervalSeconds;

	public bool IsPaused
	{
		get
		{
			if (!(this._pauseTimer > Time.time))
			{
				return this.IsOutOfRespawns;
			}
			return true;
		}
	}

	public float PauseTimeLeft => Mathf.Max(this._pauseTimer - Time.time, 0f);

	private bool IsOutOfRespawns
	{
		get
		{
			if (this._wave is ILimitedWave limitedWave)
			{
				return limitedWave.RespawnTokens <= 0;
			}
			return false;
		}
	}

	public WaveTimer(TimeBasedWave wave)
	{
		this.DefaultSpawnInterval = wave.InitialSpawnInterval;
		this._wave = wave;
		StaticUnityMethods.OnUpdate += Update;
		SeedSynchronizer.OnGenerationFinished += OnMapGenerated;
		WaveManager.OnWaveUpdateMsgReceived += OnUpdateMessageReceived;
	}

	public void AddTime(float seconds)
	{
		this.SetTime(this.TimePassed + seconds);
	}

	public void SetTime(float seconds)
	{
		this.TimePassed = seconds;
		if (NetworkServer.active)
		{
			WaveUpdateMessage.ServerSendUpdate(this._wave, UpdateMessageFlags.Timer);
		}
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
		if (NetworkServer.active)
		{
			WaveUpdateMessage.ServerSendUpdate(this._wave, UpdateMessageFlags.Pause);
		}
	}

	public void Destroy()
	{
		StaticUnityMethods.OnUpdate -= Update;
		SeedSynchronizer.OnGenerationFinished -= OnMapGenerated;
		WaveManager.OnWaveUpdateMsgReceived -= OnUpdateMessageReceived;
	}

	private void Update()
	{
		if ((!this.IsPaused || !(this.TimeLeft <= 30f)) && RoundSummary.RoundInProgress())
		{
			this.TimePassed += Time.deltaTime;
		}
	}

	private void OnUpdateMessageReceived(WaveUpdateMessage msg)
	{
		if (this._wave == msg.Wave)
		{
			if (msg.SpawnIntervalSeconds.HasValue)
			{
				this.SpawnIntervalSeconds = msg.SpawnIntervalSeconds.Value;
			}
			if (msg.TimePassed.HasValue)
			{
				this.TimePassed = msg.TimePassed.Value;
			}
			if (msg.PauseDuration.HasValue)
			{
				this._pauseTimer = msg.PauseDuration.Value;
			}
		}
	}

	private void OnMapGenerated()
	{
		this.Reset();
	}
}
