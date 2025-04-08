using System;
using VoiceChat.Networking;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryPreviewPlayback : VoiceChatPlaybackBase
	{
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

		protected override float ReadSample()
		{
			return this._playback.Read();
		}

		public bool IsEmpty
		{
			get
			{
				return !this._playbackSet || this._playback.ReadHead == this._playback.WriteHead;
			}
		}

		public void StartPreview(PlaybackBuffer pb, int startIndex, int length)
		{
			if (this._playbackSet)
			{
				this._playback.Clear();
			}
			else
			{
				this._playback = new PlaybackBuffer(pb.Buffer.Length, true);
				this._playbackSet = true;
			}
			this._playback.Write(pb.Buffer, length, startIndex);
		}

		public void StopPreview()
		{
			if (!this._playbackSet)
			{
				return;
			}
			this._playback.Clear();
		}

		private PlaybackBuffer _playback;

		private bool _playbackSet;
	}
}
