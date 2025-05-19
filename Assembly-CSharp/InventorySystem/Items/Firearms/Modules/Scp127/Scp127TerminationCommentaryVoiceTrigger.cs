using System;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127TerminationCommentaryVoiceTrigger : Scp127CassieBasedVoiceTriggerBase
{
	[Serializable]
	private struct TerminationCommentary
	{
		public RoleTypeId Role;

		public AudioClip[] Lines;
	}

	private AudioClip[] _scheduledCollection;

	[SerializeField]
	private AudioClip _cassieContainedSuccessfullyClip;

	[SerializeField]
	private TerminationCommentary[] _commentary;

	[SerializeField]
	private AudioClip[] _genericLines;

	protected override float DefaultAudioVolume => 1f;

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		PlayerStats.OnAnyPlayerDied -= OnAnyPlayerDied;
	}

	protected override bool TryIdentifyLine(NineTailedFoxAnnouncer.VoiceLine line)
	{
		return line.clip == _cassieContainedSuccessfullyClip;
	}

	private void ServerPlayScheduled()
	{
		if (_scheduledCollection != null && _scheduledCollection.Length != 0)
		{
			ServerPlayVoiceLine(_scheduledCollection.RandomItem());
		}
	}

	private void OnAnyPlayerDied(ReferenceHub hub, DamageHandlerBase dhb)
	{
		if (!base.Idle || !hub.IsSCP(includeZombies: false))
		{
			return;
		}
		RoleTypeId roleId = hub.GetRoleId();
		TerminationCommentary[] commentary = _commentary;
		for (int i = 0; i < commentary.Length; i++)
		{
			TerminationCommentary terminationCommentary = commentary[i];
			if (terminationCommentary.Role == roleId)
			{
				_scheduledCollection = terminationCommentary.Lines;
				ServerScheduleEvent(ServerPlayScheduled);
				return;
			}
		}
		_scheduledCollection = _genericLines;
		ServerScheduleEvent(ServerPlayScheduled);
	}
}
