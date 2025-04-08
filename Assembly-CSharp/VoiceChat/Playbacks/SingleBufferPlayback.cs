using System;
using VoiceChat.Networking;

namespace VoiceChat.Playbacks
{
	public class SingleBufferPlayback : VoiceChatPlaybackBase
	{
		public PlaybackBuffer Buffer
		{
			get
			{
				if (!this._bufferSet)
				{
					this._buffer = new PlaybackBuffer(24000, false);
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
			if (!this._bufferSet)
			{
				return;
			}
			this.Buffer.Dispose();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (!this._bufferSet)
			{
				return;
			}
			this.Buffer.Clear();
		}

		protected override float ReadSample()
		{
			return this.Buffer.Read();
		}

		private PlaybackBuffer _buffer;

		private bool _bufferSet;
	}
}
