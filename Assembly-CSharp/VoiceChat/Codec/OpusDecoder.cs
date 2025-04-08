using System;
using VoiceChat.Codec.Enums;

namespace VoiceChat.Codec
{
	public class OpusDecoder : IDisposable
	{
		public OpusDecoder()
		{
			this._handle = OpusWrapper.CreateDecoder(48000, 1);
			if (this._handle == IntPtr.Zero)
			{
				throw new OpusException(OpusStatusCode.AllocFail, "Memory was not allocated for the encoder");
			}
		}

		public int Decode(byte[] packetData, int dataLength, float[] samples)
		{
			if (OpusWrapper.GetBandwidth(packetData) < 0)
			{
				this._previousPacketInvalid = true;
				return OpusWrapper.Decode(this._handle, null, 0, samples, false, 1);
			}
			this._previousPacketInvalid = false;
			return OpusWrapper.Decode(this._handle, packetData, dataLength, samples, this._previousPacketInvalid, 1);
		}

		public void Dispose()
		{
			if (this._handle != IntPtr.Zero)
			{
				OpusWrapper.Destroy(this._handle);
				this._handle = IntPtr.Zero;
			}
		}

		private IntPtr _handle = IntPtr.Zero;

		private bool _previousPacketInvalid;
	}
}
