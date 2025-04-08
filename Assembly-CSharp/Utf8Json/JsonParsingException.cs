using System;
using Utf8Json.Internal;

namespace Utf8Json
{
	public class JsonParsingException : Exception
	{
		public int Offset { get; private set; }

		public string ActualChar { get; set; }

		public JsonParsingException(string message)
			: base(message)
		{
		}

		public JsonParsingException(string message, byte[] underlyingBytes, int offset, int limit, string actualChar)
			: base(message)
		{
			this.underyingBytes = new WeakReference(underlyingBytes);
			this.Offset = offset;
			this.ActualChar = actualChar;
			this.limit = limit;
		}

		public byte[] GetUnderlyingByteArrayUnsafe()
		{
			return this.underyingBytes.Target as byte[];
		}

		public string GetUnderlyingStringUnsafe()
		{
			byte[] array = this.underyingBytes.Target as byte[];
			if (array != null)
			{
				return StringEncoding.UTF8.GetString(array, 0, this.limit) + "...";
			}
			return null;
		}

		private WeakReference underyingBytes;

		private int limit;
	}
}
