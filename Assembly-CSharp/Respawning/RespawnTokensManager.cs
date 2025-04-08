using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Respawning
{
	public static class RespawnTokensManager
	{
		public static int AvailableRespawnsLeft { get; set; }

		public static Dictionary<Faction, List<RespawnTokensManager.Milestone>> Milestones { get; }

		private static List<RespawnTokensManager.Milestone> DefaultMilestone
		{
			get
			{
				return new List<RespawnTokensManager.Milestone>
				{
					new RespawnTokensManager.Milestone(30),
					new RespawnTokensManager.Milestone(80),
					new RespawnTokensManager.Milestone(150),
					new RespawnTokensManager.Milestone(200)
				};
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			WaveManager.OnWaveUpdateMsgReceived += RespawnTokensManager.OnUpdateReceived;
			WaveManager.OnWaveSpawned += RespawnTokensManager.OnWaveSpawned;
			FactionInfluenceManager.InfluenceModified += RespawnTokensManager.OnPointsModified;
			CustomNetworkManager.OnClientReady += delegate
			{
				foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
				{
					ILimitedWave limitedWave = spawnableWaveBase as ILimitedWave;
					if (limitedWave != null)
					{
						limitedWave.RespawnTokens = limitedWave.InitialRespawnTokens;
					}
				}
				if (!NetworkServer.active)
				{
					return;
				}
				RespawnTokensManager.AvailableRespawnsLeft = 3;
				RespawnTokensManager.ResetMilestones();
			};
		}

		public static bool TryGetNextThreshold(Faction faction, float influence, out int threshold)
		{
			RespawnTokensManager.Milestone milestone = RespawnTokensManager.Milestones[faction].FirstOrDefault((RespawnTokensManager.Milestone x) => influence < (float)x.Threshold, null);
			if (milestone == null)
			{
				threshold = -1;
				return false;
			}
			threshold = milestone.Threshold;
			return true;
		}

		private static void OnWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> _)
		{
			ILimitedWave limitedWave = wave as ILimitedWave;
			if (limitedWave == null)
			{
				return;
			}
			ILimitedWave limitedWave2 = limitedWave;
			int respawnTokens = limitedWave2.RespawnTokens;
			limitedWave2.RespawnTokens = respawnTokens - 1;
			WaveUpdateMessage.ServerSendUpdate(wave, UpdateMessageFlags.Tokens);
		}

		private static void ResetMilestones()
		{
			foreach (List<RespawnTokensManager.Milestone> list in RespawnTokensManager.Milestones.Values)
			{
				foreach (RespawnTokensManager.Milestone milestone in list)
				{
					milestone.Achieved = false;
				}
			}
		}

		private static bool TryAchieveMilestone(Faction faction, float influence)
		{
			foreach (RespawnTokensManager.Milestone milestone in RespawnTokensManager.Milestones[faction])
			{
				if (!milestone.Achieved && (float)milestone.Threshold <= influence)
				{
					milestone.Achieved = true;
					return true;
				}
			}
			return false;
		}

		private static void OnPointsModified(Faction faction, float newValue)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (RespawnTokensManager.AvailableRespawnsLeft == 0)
			{
				return;
			}
			SpawnableWaveBase spawnableWaveBase;
			if (!WaveManager.TryGet(faction, out spawnableWaveBase))
			{
				return;
			}
			ILimitedWave limitedWave = spawnableWaveBase as ILimitedWave;
			if (limitedWave == null)
			{
				return;
			}
			while (RespawnTokensManager.TryAchieveMilestone(faction, newValue))
			{
				ILimitedWave limitedWave2 = limitedWave;
				int respawnTokens = limitedWave2.RespawnTokens;
				limitedWave2.RespawnTokens = respawnTokens + 1;
				RespawnTokensManager.AvailableRespawnsLeft--;
				WaveUpdateMessage.ServerSendUpdate(spawnableWaveBase, UpdateMessageFlags.Tokens);
				if (RespawnTokensManager.AvailableRespawnsLeft == 0)
				{
					break;
				}
			}
			WaveUpdateMessage.ServerSendUpdate(spawnableWaveBase, UpdateMessageFlags.Tokens);
		}

		private static void OnUpdateReceived(WaveUpdateMessage msg)
		{
			if (msg.RespawnTokens == null)
			{
				return;
			}
			ILimitedWave limitedWave = msg.Wave as ILimitedWave;
			if (limitedWave == null)
			{
				return;
			}
			limitedWave.RespawnTokens = msg.RespawnTokens.Value;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static RespawnTokensManager()
		{
			Dictionary<Faction, List<RespawnTokensManager.Milestone>> dictionary = new Dictionary<Faction, List<RespawnTokensManager.Milestone>>();
			dictionary[Faction.FoundationStaff] = RespawnTokensManager.DefaultMilestone;
			dictionary[Faction.FoundationEnemy] = RespawnTokensManager.DefaultMilestone;
			RespawnTokensManager.Milestones = dictionary;
		}

		public const int DefaultTokensCount = 1;

		private const int InitialRespawns = 3;

		public class Milestone
		{
			public Milestone(int threshold)
			{
				this.Threshold = threshold;
			}

			public int Threshold;

			public bool Achieved;
		}
	}
}
