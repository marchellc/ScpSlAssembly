using VoiceChat.Networking;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryPreviewPlayback : VoiceChatPlaybackBase
{
	private PlaybackBuffer _playback;

	private bool _playbackSet;

	public override int MaxSamples
	{
		get
		{
			if (!this._playbackSet)
			{
				return 0;
			}
			return this._playback.Length;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (this._playbackSet)
			{
				return this._playback.ReadHead == this._playback.WriteHead;
			}
			return true;
		}
	}

	protected override float ReadSample()
	{
		return this._playback.Read();
	}

	public void StartPreview(PlaybackBuffer pb, int startIndex, int length)
	{
		if (this._playbackSet)
		{
			this._playback.Clear();
		}
		else
		{
			this._playback = new PlaybackBuffer(pb.Buffer.Length, endlessTapeMode: true);
			this._playbackSet = true;
		}
		this._playback.Write(pb.Buffer, length, startIndex);
	}

	public void StopPreview()
	{
		if (this._playbackSet)
		{
			this._playback.Clear();
		}
	}
}
