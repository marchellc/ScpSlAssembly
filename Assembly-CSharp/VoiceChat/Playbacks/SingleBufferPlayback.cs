using VoiceChat.Networking;

namespace VoiceChat.Playbacks;

public class SingleBufferPlayback : VoiceChatPlaybackBase
{
	private PlaybackBuffer _buffer;

	private bool _bufferSet;

	public PlaybackBuffer Buffer
	{
		get
		{
			if (!this._bufferSet)
			{
				this._buffer = new PlaybackBuffer();
				this._bufferSet = true;
			}
			return this._buffer;
		}
	}

	public override int MaxSamples
	{
		get
		{
			if (!this._bufferSet)
			{
				return 0;
			}
			return this.Buffer.Length;
		}
	}

	private void OnDestroy()
	{
		if (this._bufferSet)
		{
			this.Buffer.Dispose();
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (this._bufferSet)
		{
			this.Buffer.Clear();
		}
	}

	protected override float ReadSample()
	{
		return this.Buffer.Read();
	}
}
