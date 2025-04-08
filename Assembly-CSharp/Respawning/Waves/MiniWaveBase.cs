using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using Respawning.Waves.Generic;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Respawning.Waves
{
	public abstract class MiniWaveBase<TMainWave> : TimeBasedWave, IMiniWave, ILimitedWave where TMainWave : SpawnableWaveBase
	{
		public override float InitialSpawnInterval
		{
			get
			{
				return 150f;
			}
		}

		public float WaveSizeMultiplier { get; set; } = 0.2f;

		public override int MaxWaveSize
		{
			get
			{
				return Mathf.CeilToInt((float)ReferenceHub.AllHubs.Count * this.WaveSizeMultiplier);
			}
		}

		public override bool ReceiveObjectiveRewards
		{
			get
			{
				return false;
			}
		}

		public override bool PauseOnTrigger
		{
			get
			{
				return false;
			}
		}

		public int InitialRespawnTokens { get; set; }

		public int RespawnTokens { get; set; }

		public virtual RoleTypeId DefaultRole { get; set; }

		public virtual RoleTypeId SpecialRole { get; set; }

		public void Unlock(bool ignoreConfig = false)
		{
			if (!ignoreConfig && NetworkServer.active && !this.Configuration.IsEnabled)
			{
				return;
			}
			this.RespawnTokens = 1;
			WaveUpdateMessage.ServerSendUpdate(this, UpdateMessageFlags.Tokens);
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
			TMainWave tmainWave;
			if (wave == this && WaveManager.TryGet<TMainWave>(out tmainWave))
			{
				TimeBasedWave timeBasedWave = tmainWave as TimeBasedWave;
				if (timeBasedWave != null)
				{
					this.Unlock(false);
					timeBasedWave.Timer.AddTime(-60f);
					return;
				}
			}
			this._mainWaveSpawned = wave is TMainWave;
			if (!this._mainWaveSpawned)
			{
				this.ResetTokens();
				return;
			}
			int num = ReferenceHub.AllHubs.Count(delegate(ReferenceHub h)
			{
				SpectatorRole spectatorRole = h.roleManager.CurrentRole as SpectatorRole;
				return spectatorRole != null && spectatorRole.ReadyToRespawn;
			});
			float num2 = 2f * (float)num;
			this._availabilityTimer = Time.time + 120f;
			this._cachedInfluenceCount = FactionInfluenceManager.Get(this.TargetFaction) - num2;
			base.Timer.Reset(true);
		}

		public override void OnWaveSpawned()
		{
		}

		protected override void OnInstanceCreated()
		{
			base.OnInstanceCreated();
			PlayerStats.OnAnyPlayerDied += this.OnPlayerDeath;
			FactionInfluenceManager.InfluenceModified += this.OnInfluenceModified;
		}

		protected override void OnInstanceReset()
		{
			base.OnInstanceReset();
			this._availabilityTimer = 0f;
			this._cachedInfluenceCount = 0f;
			this._mainWaveSpawned = false;
		}

		protected override void OnInstanceDestroyed()
		{
			base.OnInstanceDestroyed();
			PlayerStats.OnAnyPlayerDied -= this.OnPlayerDeath;
			FactionInfluenceManager.InfluenceModified -= this.OnInfluenceModified;
		}

		private void OnPlayerDeath(ReferenceHub victim, DamageHandlerBase dh)
		{
			if (!this._mainWaveSpawned)
			{
				return;
			}
			if (base.Timer.TimeLeft <= 90f)
			{
				return;
			}
			if (victim.GetFaction() != this.TargetFaction)
			{
				return;
			}
			base.Timer.AddTime(5f);
		}

		private void OnInfluenceModified(Faction faction, float newValue)
		{
			if (!this._mainWaveSpawned)
			{
				return;
			}
			if (this.TargetFaction != faction)
			{
				return;
			}
			if (this._availabilityTimer <= Time.time)
			{
				return;
			}
			if (newValue - this._cachedInfluenceCount < 10f)
			{
				return;
			}
			this.Unlock(false);
		}

		public const float TimeLeftReductionThreshold = 90f;

		public const float TeamDeathSecondsCompensation = 5f;

		public const float InfluenceNeededToUnlock = 10f;

		public const float SecondsIncreasePenalization = 60f;

		public const float AvailabilityDuration = 120f;

		public const float InfluencePerSpectator = 2f;

		private float _cachedInfluenceCount;

		private float _availabilityTimer;

		private bool _mainWaveSpawned;
	}
}
