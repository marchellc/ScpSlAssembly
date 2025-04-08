using System;
using System.Collections.Generic;
using Mirror;

namespace Respawning.Waves
{
	public abstract class TimeBasedWave : SpawnableWaveBase
	{
		public abstract float InitialSpawnInterval { get; }

		public WaveTimer Timer { get; private set; }

		public virtual bool ReceiveObjectiveRewards
		{
			get
			{
				return true;
			}
		}

		public virtual bool PauseOnTrigger
		{
			get
			{
				return true;
			}
		}

		public virtual bool IsReadyToSpawn
		{
			get
			{
				return this.Timer.IsReadyToSpawn;
			}
		}

		protected override void OnInstanceCreated()
		{
			base.OnInstanceCreated();
			this.Timer = new WaveTimer(this);
		}

		protected override void OnInstanceDestroyed()
		{
			base.OnInstanceDestroyed();
			WaveTimer timer = this.Timer;
			if (timer == null)
			{
				return;
			}
			timer.Destroy();
		}

		protected override void OnAnyWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> spawnedPlayers)
		{
			base.OnAnyWaveSpawned(wave, spawnedPlayers);
			if (!NetworkServer.active || wave != this)
			{
				return;
			}
			this.Timer.SpawnIntervalSeconds += (float)spawnedPlayers.Count * this.AdditionalSecondsPerSpawn;
			WaveUpdateMessage.ServerSendUpdate(this, UpdateMessageFlags.Timer);
		}

		public float AdditionalSecondsPerSpawn = 10f;
	}
}
