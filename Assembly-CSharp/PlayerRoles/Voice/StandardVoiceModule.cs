using System.Diagnostics;
using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.Voice;

public abstract class StandardVoiceModule : VoiceModuleBase, IGlobalPlayback
{
	private const float SustainTime = 0.2f;

	private bool _primHeld;

	private bool _altHeld;

	private readonly Stopwatch _primSw = new Stopwatch();

	private readonly Stopwatch _altSw = new Stopwatch();

	[SerializeField]
	protected SingleBufferPlayback GlobalPlayback;

	public virtual bool GlobalChatActive => this.GlobalPlayback.MaxSamples > 0;

	public virtual Color GlobalChatColor => base.Owner.serverRoles.GetVoiceColor();

	public virtual string GlobalChatName => base.Owner.nicknameSync.DisplayName;

	public virtual float GlobalChatLoudness => this.GlobalPlayback.Loudness;

	public virtual GlobalChatIconType GlobalChatIcon
	{
		get
		{
			if (!this.IsRoundSummary)
			{
				return GlobalChatIconType.Avatar;
			}
			return GlobalChatIconType.None;
		}
	}

	protected bool IsRoundSummary => RoundSummary.SummaryActive;

	public override VoiceChatChannel GetUserInput()
	{
		KeyCode key = NewInput.GetKey(ActionName.VoiceChat);
		KeyCode key2 = NewInput.GetKey(ActionName.AltVoiceChat);
		return this.ProcessInputs(this.ProcessKey(key, ref this._primHeld, this._primSw), this.ProcessKey(key2, ref this._altHeld, this._altSw));
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		GlobalChatIndicatorManager.Subscribe(this, base.Owner);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._primHeld = false;
		this._altHeld = false;
		this._primSw.Stop();
		this._altSw.Stop();
		GlobalChatIndicatorManager.Unsubscribe(this);
	}

	public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
	{
		if (speaker == base.Owner)
		{
			return VoiceChatChannel.None;
		}
		if (channel == VoiceChatChannel.Mimicry)
		{
			return channel;
		}
		if (this.IsRoundSummary && (base.ReceiveFlags & GroupMuteFlags.Summary) == 0)
		{
			return VoiceChatChannel.RoundSummary;
		}
		if (Intercom.CheckPerms(speaker) && channel != VoiceChatChannel.Scp1576)
		{
			return VoiceChatChannel.Intercom;
		}
		return channel;
	}

	protected override void ProcessSamples(float[] data, int len)
	{
		switch (base.CurrentChannel)
		{
		case VoiceChatChannel.RoundSummary:
			this.GlobalPlayback.Buffer.Write(data, len);
			break;
		case VoiceChatChannel.Intercom:
			IntercomPlayback.ProcessSamples(base.Owner, data, len);
			break;
		}
	}

	protected abstract VoiceChatChannel ProcessInputs(bool primary, bool alt);

	private bool ProcessKey(KeyCode kc, ref bool prev, Stopwatch sw)
	{
		if (Input.GetKeyDown(kc))
		{
			prev = true;
		}
		if (!Input.GetKey(kc))
		{
			prev = false;
		}
		if (prev)
		{
			sw.Restart();
		}
		if (sw.IsRunning)
		{
			return sw.Elapsed.TotalSeconds < 0.20000000298023224;
		}
		return false;
	}
}
