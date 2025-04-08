using System;
using VoiceChat.Codec.Enums;

namespace VoiceChat.Codec
{
	public class OpusException : Exception
	{
		public OpusException(OpusStatusCode statusCode, string message)
			: base(message)
		{
			this.StatusCode = statusCode;
		}

		public readonly OpusStatusCode StatusCode;
	}
}
