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
		return line.clip == this._cassieContainedSuccessfullyClip;
	}

	private void ServerPlayScheduled()
	{
		if (this._scheduledCollection != null && this._scheduledCollection.Length != 0)
		{
			base.ServerPlayVoiceLine(this._scheduledCollection.RandomItem());
		}
	}

	private void OnAnyPlayerDied(ReferenceHub hub, DamageHandlerBase dhb)
	{
		if (!base.Idle || !hub.IsSCP(includeZombies: false))
		{
			return;
		}
		RoleTypeId roleId = hub.GetRoleId();
		TerminationCommentary[] commentary = this._commentary;
		for (int i = 0; i < commentary.Length; i++)
		{
			TerminationCommentary terminationCommentary = commentary[i];
			if (terminationCommentary.Role == roleId)
			{
				this._scheduledCollection = terminationCommentary.Lines;
				base.ServerScheduleEvent(ServerPlayScheduled);
				return;
			}
		}
		this._scheduledCollection = this._genericLines;
		base.ServerScheduleEvent(ServerPlayScheduled);
	}
}
