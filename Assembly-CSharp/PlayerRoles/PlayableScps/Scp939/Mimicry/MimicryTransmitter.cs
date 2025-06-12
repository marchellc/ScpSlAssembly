using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;
using VoiceChat;
using VoiceChat.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryTransmitter : StandardSubroutine<Scp939Role>
{
	private PlaybackBuffer _copierPlayback;

	private PlaybackBuffer _senderPlayback;

	private int _playbackSize;

	private int _allowedSamples;

	private int _samplesPerSecond;

	private const int HeadSamples = 1920;

	public bool IsTransmitting
	{
		get
		{
			PlaybackBuffer copierPlayback = this._copierPlayback;
			if (copierPlayback == null || copierPlayback.Length <= 0)
			{
				PlaybackBuffer senderPlayback = this._senderPlayback;
				if (senderPlayback == null)
				{
					return false;
				}
				return senderPlayback.Length > 0;
			}
			return true;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._samplesPerSecond = 48000;
	}

	public void SendVoice(PlaybackBuffer pb, int startSample, int maxLength)
	{
		pb.Reorganize();
		int num = pb.Buffer.Length;
		if (this._playbackSize < num)
		{
			this._copierPlayback = new PlaybackBuffer(num);
			this._senderPlayback = new PlaybackBuffer(num);
			this._playbackSize = num;
		}
		else
		{
			this._copierPlayback.Clear();
			this._senderPlayback.Clear();
		}
		this._allowedSamples = 1920;
		this._copierPlayback.Write(pb.Buffer, Mathf.Min(maxLength, pb.Length), startSample);
	}

	public void StopTransmission()
	{
		if (this.IsTransmitting)
		{
			this._copierPlayback?.Clear();
			this._senderPlayback?.Clear();
			base.ClientSendCmd();
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		base.ServerSendRpc(toAll: true);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		(base.CastRole.VoiceModule as Scp939VoiceModule).ClearMimicryPlayback();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._copierPlayback?.Clear();
		this._senderPlayback?.Clear();
	}

	private void Update()
	{
		if (base.Owner.isLocalPlayer && this._playbackSize != 0)
		{
			this._allowedSamples += Mathf.CeilToInt(Time.deltaTime * (float)this._samplesPerSecond);
			int num = Mathf.Min(this._allowedSamples, this._copierPlayback.Length);
			if (num > 0)
			{
				this._copierPlayback.ReadTo(this._senderPlayback.Buffer, num, this._senderPlayback.WriteHead);
				this._senderPlayback.WriteHead += num;
			}
			this._allowedSamples = 0;
			VoiceTransceiver.ClientSendData(this._senderPlayback, VoiceChatChannel.Mimicry, 1);
		}
	}
}
