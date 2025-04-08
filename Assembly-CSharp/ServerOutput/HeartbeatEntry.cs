using System;

namespace ServerOutput
{
	public struct HeartbeatEntry : IOutputEntry
	{
		public string GetString()
		{
			return 23.ToString();
		}

		public int GetBytesLength()
		{
			return 1;
		}

		public void GetBytes(ref byte[] buffer, out int length)
		{
			length = 1;
			buffer[0] = 23;
		}
	}
}
