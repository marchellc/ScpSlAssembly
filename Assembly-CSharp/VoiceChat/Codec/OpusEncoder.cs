using System;
using VoiceChat.Codec.Enums;

namespace VoiceChat.Codec
{
	public class OpusEncoder : IDisposable
	{
		public OpusEncoder(OpusApplicationType preset)
		{
			this._handle = OpusWrapper.CreateEncoder(48000, 1, preset);
			OpusWrapper.SetEncoderSetting(this._handle, OpusCtlSetRequest.Bitrate, 120000);
		}

		public int Encode(float[] pcmSamples, byte[] encoded, int frameSize = 480)
		{
			return OpusWrapper.Encode(this._handle, pcmSamples, frameSize, encoded);
		}

		public void Dispose()
		{
			if (this._handle == IntPtr.Zero)
			{
				return;
			}
			OpusWrapper.Destroy(this._handle);
			this._handle = IntPtr.Zero;
		}

		private IntPtr _handle = IntPtr.Zero;
	}
}
