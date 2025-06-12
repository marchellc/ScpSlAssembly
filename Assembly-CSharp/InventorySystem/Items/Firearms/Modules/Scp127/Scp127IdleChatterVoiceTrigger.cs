using System.Diagnostics;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127IdleChatterVoiceTrigger : Scp127VoiceTriggerBase
{
	private readonly Stopwatch _idleTimer = new Stopwatch();

	private float _nextLineSeconds;

	[SerializeField]
	private float _minIdleTimeMinutes;

	[SerializeField]
	private float _maxIdleTimeMinutes;

	[SerializeField]
	private Scp127VoiceLineCollection _voiceLines;

	private void ReRandomizeTimer()
	{
		this._idleTimer.Restart();
		this._nextLineSeconds = Random.Range(this._minIdleTimeMinutes, this._maxIdleTimeMinutes) * 60f;
	}

	private void ResetIdleTimer(Firearm firearm)
	{
		if (!(firearm != base.Firearm))
		{
			this._idleTimer.Restart();
		}
	}

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		Scp127VoiceLineManagerModule.OnServerVoiceLineSent += ResetIdleTimer;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		Scp127VoiceLineManagerModule.OnServerVoiceLineSent -= ResetIdleTimer;
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.HasFriendship)
		{
			this.ReRandomizeTimer();
		}
	}

	public override void OnFriendshipCreated()
	{
		base.OnFriendshipCreated();
		this.ReRandomizeTimer();
	}

	internal override void AlwaysUpdate()
	{
		base.AlwaysUpdate();
		if (!(this._idleTimer.Elapsed.TotalSeconds < (double)this._nextLineSeconds))
		{
			this.ReRandomizeTimer();
			base.ServerPlayVoiceLineFromCollection(this._voiceLines);
		}
	}
}
