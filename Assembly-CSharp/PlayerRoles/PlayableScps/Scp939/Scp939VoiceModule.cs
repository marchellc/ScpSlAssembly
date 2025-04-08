using System;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939VoiceModule : StandardScpVoiceModule
	{
		protected override OpusDecoder Decoder
		{
			get
			{
				if (base.CurrentChannel != VoiceChatChannel.Mimicry)
				{
					return base.Decoder;
				}
				if (!this._mimicryDecoderSet)
				{
					this._mimicryDecoder = new OpusDecoder();
					this._mimicryDecoderSet = true;
				}
				return this._mimicryDecoder;
			}
		}

		protected override void ProcessSamples(float[] data, int len)
		{
			if (base.CurrentChannel == VoiceChatChannel.Mimicry)
			{
				this._proximityChat.Buffer.Write(data, len);
				return;
			}
			base.ProcessSamples(data, len);
		}

		public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
		{
			if (channel != VoiceChatChannel.Mimicry)
			{
				return base.ValidateReceive(speaker, channel);
			}
			return channel;
		}

		public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
		{
			if (channel != VoiceChatChannel.Mimicry)
			{
				return base.ValidateSend(channel);
			}
			return channel;
		}

		public void ClearMimicryPlayback()
		{
			this._proximityChat.Buffer.Clear();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			if (!this._mimicryDecoderSet)
			{
				return;
			}
			OpusDecoder mimicryDecoder = this._mimicryDecoder;
			if (mimicryDecoder != null)
			{
				mimicryDecoder.Dispose();
			}
			this._mimicryDecoderSet = false;
		}

		[SerializeField]
		private SingleBufferPlayback _proximityChat;

		private OpusDecoder _mimicryDecoder;

		private bool _mimicryDecoderSet;

		public const VoiceChatChannel MimicryChannel = VoiceChatChannel.Mimicry;
	}
}
