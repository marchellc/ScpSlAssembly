using System;
using System.Collections.Generic;
using System.Text;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127SpawnWaveVoiceTrigger : Scp127CassieBasedVoiceTriggerBase
{
	[Serializable]
	private struct WaveLinePair
	{
		public Faction WaveFaction;

		public Scp127VoiceLineCollection Collection;
	}

	[SerializeField]
	private WaveLinePair[] _voiceLines;

	private static readonly StringBuilder TempSb = new StringBuilder();

	private readonly Queue<string> _remainingKeywords = new Queue<string>();

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		WaveManager.OnWaveSpawned += OnWaveSpawned;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		WaveManager.OnWaveSpawned -= OnWaveSpawned;
	}

	protected override bool TryIdentifyLine(NineTailedFoxAnnouncer.VoiceLine line)
	{
		if (!this._remainingKeywords.TryPeek(out var result))
		{
			return true;
		}
		if (!string.Equals(line.apiName, result, StringComparison.InvariantCultureIgnoreCase))
		{
			return false;
		}
		this._remainingKeywords.Dequeue();
		return this._remainingKeywords.IsEmpty();
	}

	private void OnWaveSpawned(SpawnableWaveBase wave, List<ReferenceHub> spawnedPlayers)
	{
		if (!base.Idle || !(wave is IAnnouncedWave announcedWave))
		{
			return;
		}
		WaveLinePair? foundPair = null;
		WaveLinePair[] voiceLines = this._voiceLines;
		for (int i = 0; i < voiceLines.Length; i++)
		{
			WaveLinePair value = voiceLines[i];
			if (value.WaveFaction == wave.TargetFaction)
			{
				foundPair = value;
				break;
			}
		}
		if (foundPair.HasValue)
		{
			base.ServerScheduleEvent(delegate
			{
				base.ServerPlayVoiceLineFromCollection(foundPair.Value.Collection);
			});
			Scp127SpawnWaveVoiceTrigger.TempSb.Clear();
			this._remainingKeywords.Clear();
			announcedWave.Announcement.CreateAnnouncementString(Scp127SpawnWaveVoiceTrigger.TempSb);
			string[] array = Scp127SpawnWaveVoiceTrigger.TempSb.ToString().Split(' ');
			foreach (string item in array)
			{
				this._remainingKeywords.Enqueue(item);
			}
		}
	}
}
