using System;
using AudioPooling;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public abstract class Scp127CassieBasedVoiceTriggerBase : Scp127VoiceTriggerBase
{
	private enum DetectionStatus
	{
		Idle,
		WaitingForTrigger,
		AnnouncementStartedPlaying
	}

	[Serializable]
	private struct TerminationCommentary
	{
		public RoleTypeId Role;

		public AudioClip[] Lines;
	}

	private Action _scheduledCallback;

	private DetectionStatus _status;

	protected override MixerChannel DefaultAudioMixerChannel => MixerChannel.NoDucking;

	protected bool Idle => this._status == DetectionStatus.Idle;

	internal override void OnClientReady()
	{
		base.OnClientReady();
		this._status = DetectionStatus.Idle;
	}

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		NineTailedFoxAnnouncer.OnLineDequeued += OnCassieLineDequeued;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		NineTailedFoxAnnouncer.OnLineDequeued -= OnCassieLineDequeued;
	}

	protected void ServerScheduleEvent(Action callback)
	{
		if (NetworkServer.active)
		{
			this._scheduledCallback = callback;
			this._status = DetectionStatus.WaitingForTrigger;
		}
	}

	protected abstract bool TryIdentifyLine(NineTailedFoxAnnouncer.VoiceLine line);

	private void OnCassieLineDequeued(NineTailedFoxAnnouncer.VoiceLine line)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		switch (this._status)
		{
		case DetectionStatus.WaitingForTrigger:
			if (this.TryIdentifyLine(line))
			{
				this._status = DetectionStatus.AnnouncementStartedPlaying;
			}
			break;
		case DetectionStatus.AnnouncementStartedPlaying:
			if (line.apiName == "END_OF_MESSAGE")
			{
				this._status = DetectionStatus.Idle;
				this._scheduledCallback?.Invoke();
			}
			break;
		}
	}
}
