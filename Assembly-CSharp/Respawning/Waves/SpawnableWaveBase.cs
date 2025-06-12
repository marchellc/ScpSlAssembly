using System.Collections.Generic;
using PlayerRoles;
using Respawning.Config;

namespace Respawning.Waves;

public abstract class SpawnableWaveBase
{
	public abstract int MaxWaveSize { get; }

	public abstract Faction TargetFaction { get; }

	public abstract IWaveConfig Configuration { get; }

	public SpawnableWaveBase()
	{
		this.OnInstanceCreated();
		CustomNetworkManager.OnClientReady += OnInstanceReset;
		WaveManager.OnWaveSpawned += OnAnyWaveSpawned;
	}

	public abstract void PopulateQueue(Queue<RoleTypeId> queueToFill, int playersToSpawn);

	public virtual void OnWaveSpawned()
	{
		foreach (TimeBasedWave wave in WaveManager.Waves)
		{
			bool resetSpawnInterval = wave == this;
			wave.Timer.Reset(resetSpawnInterval);
		}
	}

	public void Destroy()
	{
		this.OnInstanceDestroyed();
		CustomNetworkManager.OnClientReady -= OnInstanceReset;
		WaveManager.OnWaveSpawned -= OnAnyWaveSpawned;
	}

	protected virtual void OnAnyWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> spawnedPlayers)
	{
	}

	protected virtual void OnInstanceCreated()
	{
	}

	protected virtual void OnInstanceReset()
	{
	}

	protected virtual void OnInstanceDestroyed()
	{
	}
}
