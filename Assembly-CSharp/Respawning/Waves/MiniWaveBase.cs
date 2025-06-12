using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using Respawning.Waves.Generic;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Respawning.Waves;

public abstract class MiniWaveBase<TMainWave, TCounterWave> : TimeBasedWave, IMiniWave, ILimitedWave where TMainWave : SpawnableWaveBase where TCounterWave : SpawnableWaveBase
{
	public const float TimeLeftReductionThreshold = 120f;

	public const float TeamDeathSecondsCompensation = 5f;

	public const float InfluenceNeededToUnlock = 10f;

	public const float SecondsIncreasePenalization = 60f;

	public const float AvailabilityDuration = 120f;

	public const float InfluencePerSpectator = 2f;

	private float _cachedInfluenceCount;

	private float _availabilityTimer;

	private bool _mainWaveSpawned;

	private bool _waveFailedObjective;

	public override float InitialSpawnInterval => 150f;

	public float WaveSizeMultiplier { get; set; } = 0.2f;

	public override int MaxWaveSize => Mathf.CeilToInt((float)ReferenceHub.AllHubs.Count * this.WaveSizeMultiplier);

	public override bool ReceiveObjectiveRewards => false;

	public override bool PauseOnTrigger => false;

	public int InitialRespawnTokens { get; set; }

	public int RespawnTokens { get; set; }

	public virtual RoleTypeId DefaultRole { get; set; }

	public virtual RoleTypeId SpecialRole { get; set; }

	public void Unlock(bool ignoreConfig = false)
	{
		if (ignoreConfig || !NetworkServer.active || this.Configuration.IsEnabled)
		{
			this.RespawnTokens = 1;
			WaveUpdateMessage.ServerSendUpdate(this, UpdateMessageFlags.Tokens);
		}
	}

	public void ResetTokens()
	{
		this.RespawnTokens = this.InitialRespawnTokens;
		WaveUpdateMessage.ServerSendUpdate(this, UpdateMessageFlags.Tokens);
	}

	public override void PopulateQueue(Queue<RoleTypeId> queueToFill, int playersToSpawn)
	{
		queueToFill.Enqueue(this.SpecialRole);
		for (int i = 0; i < playersToSpawn - 1; i++)
		{
			queueToFill.Enqueue(this.DefaultRole);
		}
	}

	protected override void OnAnyWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> _)
	{
		this._mainWaveSpawned = wave is TMainWave;
		if (wave == this && WaveManager.TryGet<TMainWave>(out var spawnWave) && spawnWave is TimeBasedWave timeBasedWave)
		{
			this.Unlock();
			timeBasedWave.Timer.AddTime(-60f);
			return;
		}
		base.Timer.Reset();
		if (!this._mainWaveSpawned)
		{
			this.ResetTokens();
			return;
		}
		int num = ReferenceHub.AllHubs.Count((ReferenceHub h) => h.roleManager.CurrentRole is SpectatorRole spectatorRole && spectatorRole.ReadyToRespawn);
		float num2 = 2f * (float)num;
		this._waveFailedObjective = false;
		this._availabilityTimer = Time.time + 120f;
		this._cachedInfluenceCount = FactionInfluenceManager.Get(this.TargetFaction) - num2;
	}

	public override void OnWaveSpawned()
	{
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		PlayerStats.OnAnyPlayerDied += OnPlayerDeath;
		FactionInfluenceManager.InfluenceModified += OnInfluenceModified;
		StaticUnityMethods.OnUpdate += OnUpdate;
	}

	protected override void OnInstanceReset()
	{
		base.OnInstanceReset();
		this._availabilityTimer = 0f;
		this._cachedInfluenceCount = 0f;
		this._mainWaveSpawned = false;
		this._waveFailedObjective = false;
	}

	protected override void OnInstanceDestroyed()
	{
		base.OnInstanceDestroyed();
		PlayerStats.OnAnyPlayerDied -= OnPlayerDeath;
		FactionInfluenceManager.InfluenceModified -= OnInfluenceModified;
		StaticUnityMethods.OnUpdate -= OnUpdate;
	}

	private void OnPlayerDeath(ReferenceHub victim, DamageHandlerBase dh)
	{
		if (this._mainWaveSpawned && !(base.Timer.TimeLeft <= 120f) && victim.GetFaction() == this.TargetFaction)
		{
			base.Timer.AddTime(5f);
		}
	}

	private void OnInfluenceModified(Faction faction, float newValue)
	{
		if (this._mainWaveSpawned && this.TargetFaction == faction && !(this._availabilityTimer <= Time.time) && !(newValue - this._cachedInfluenceCount < 10f))
		{
			this.Unlock();
		}
	}

	private void OnUpdate()
	{
		if (this._mainWaveSpawned && this.RespawnTokens <= 0 && !this._waveFailedObjective && !(this._availabilityTimer > Time.time))
		{
			this._waveFailedObjective = true;
			if (WaveManager.TryGet<TCounterWave>(out var spawnWave) && spawnWave is IMiniWave miniWave)
			{
				miniWave.Unlock();
			}
		}
	}
}
