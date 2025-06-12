using System.Collections.Generic;
using Mirror;

namespace Respawning.Waves;

public abstract class TimeBasedWave : SpawnableWaveBase
{
	public float AdditionalSecondsPerSpawn = 10f;

	public abstract float InitialSpawnInterval { get; }

	public WaveTimer Timer { get; private set; }

	public virtual bool ReceiveObjectiveRewards => true;

	public virtual bool PauseOnTrigger => true;

	public virtual bool IsReadyToSpawn => this.Timer.IsReadyToSpawn;

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		this.Timer = new WaveTimer(this);
	}

	protected override void OnInstanceDestroyed()
	{
		base.OnInstanceDestroyed();
		this.Timer?.Destroy();
	}

	protected override void OnAnyWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> spawnedPlayers)
	{
		base.OnAnyWaveSpawned(wave, spawnedPlayers);
		if (NetworkServer.active && wave == this)
		{
			this.Timer.SpawnIntervalSeconds += (float)spawnedPlayers.Count * this.AdditionalSecondsPerSpawn;
			WaveUpdateMessage.ServerSendUpdate(this, UpdateMessageFlags.Timer | UpdateMessageFlags.Spawn);
		}
	}
}
