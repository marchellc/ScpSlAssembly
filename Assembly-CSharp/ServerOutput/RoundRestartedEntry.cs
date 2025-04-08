using System;

namespace ServerOutput
{
	public struct RoundRestartedEntry : IOutputEntry
	{
		public string GetString()
		{
			return 16.ToString();
		}

		public int GetBytesLength()
		{
			return 1;
		}

		public void GetBytes(ref byte[] buffer, out int length)
		{
			length = 1;
			buffer[0] = 16;
		}
	}
}
