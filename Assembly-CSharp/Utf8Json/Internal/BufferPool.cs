using System;

namespace Utf8Json.Internal
{
	internal sealed class BufferPool : ArrayPool<byte>
	{
		public BufferPool(int bufferLength)
			: base(bufferLength)
		{
		}

		public static readonly BufferPool Default = new BufferPool(65535);
	}
}
