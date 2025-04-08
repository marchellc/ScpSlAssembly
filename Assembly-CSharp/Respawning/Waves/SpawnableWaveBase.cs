using System;
using System.Collections.Generic;
using PlayerRoles;
using Respawning.Config;

namespace Respawning.Waves
{
	public abstract class SpawnableWaveBase
	{
		public SpawnableWaveBase()
		{
			this.OnInstanceCreated();
			CustomNetworkManager.OnClientReady += this.OnInstanceReset;
			WaveManager.OnWaveSpawned += this.OnAnyWaveSpawned;
		}

		public abstract int MaxWaveSize { get; }

		public abstract Faction TargetFaction { get; }

		public abstract IWaveConfig Configuration { get; }

		public abstract void PopulateQueue(Queue<RoleTypeId> queueToFill, int playersToSpawn);

		public virtual void OnWaveSpawned()
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				bool flag = timeBasedWave == this;
				timeBasedWave.Timer.Reset(flag);
			}
		}

		public void Destroy()
		{
			this.OnInstanceDestroyed();
			CustomNetworkManager.OnClientReady -= this.OnInstanceReset;
			WaveManager.OnWaveSpawned -= this.OnAnyWaveSpawned;
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
}
