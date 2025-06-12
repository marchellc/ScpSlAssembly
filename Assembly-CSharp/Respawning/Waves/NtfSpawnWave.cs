using System.Collections.Generic;
using PlayerRoles;
using Respawning.Announcements;
using Respawning.Config;
using Respawning.Waves.Generic;
using UnityEngine;

namespace Respawning.Waves;

public class NtfSpawnWave : TimeBasedWave, IAnimatedWave, ILimitedWave, IAnnouncedWave
{
	public int MaxCaptains = 1;

	public int MaxSergeants = 3;

	public override float InitialSpawnInterval => 300f;

	public int InitialRespawnTokens { get; set; } = 1;

	public int RespawnTokens { get; set; }

	public override int MaxWaveSize
	{
		get
		{
			if (!(this.Configuration is PrimaryWaveConfig<NtfSpawnWave> primaryWaveConfig))
			{
				return 0;
			}
			return Mathf.CeilToInt((float)ReferenceHub.AllHubs.Count * primaryWaveConfig.SizePercentage);
		}
	}

	public override Faction TargetFaction => Faction.FoundationStaff;

	public float AnimationDuration => 17.95f;

	public bool IsAnimationPlaying { get; set; }

	public WaveAnnouncementBase Announcement { get; } = new NtfWaveAnnouncement(Team.FoundationForces);

	public override IWaveConfig Configuration { get; } = new PrimaryWaveConfig<NtfSpawnWave>();

	public override void PopulateQueue(Queue<RoleTypeId> queueToFill, int playersToSpawn)
	{
		for (int i = 0; i < this.MaxCaptains; i++)
		{
			queueToFill.Enqueue(RoleTypeId.NtfCaptain);
		}
		for (int j = 0; j < this.MaxSergeants; j++)
		{
			queueToFill.Enqueue(RoleTypeId.NtfSergeant);
		}
		int num = this.MaxCaptains + this.MaxSergeants;
		for (int k = 0; k < playersToSpawn - num; k++)
		{
			queueToFill.Enqueue(RoleTypeId.NtfPrivate);
		}
	}
}
