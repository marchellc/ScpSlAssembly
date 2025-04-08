using System;

namespace ServerOutput
{
	public struct IdleEnterEntry : IOutputEntry
	{
		public string GetString()
		{
			return 17.ToString();
		}

		public int GetBytesLength()
		{
			return 1;
		}

		public void GetBytes(ref byte[] buffer, out int length)
		{
			length = 1;
			buffer[0] = 17;
		}
	}
}
