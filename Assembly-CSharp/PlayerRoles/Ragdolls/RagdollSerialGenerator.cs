using System;

namespace PlayerRoles.Ragdolls
{
	public static class RagdollSerialGenerator
	{
		public static ushort GenerateNext()
		{
			object @lock = RagdollSerialGenerator.Lock;
			ushort num;
			lock (@lock)
			{
				ushort nextSerial = RagdollSerialGenerator._nextSerial;
				RagdollSerialGenerator._nextSerial = nextSerial + 1;
				num = nextSerial;
			}
			return num;
		}

		private static ushort _nextSerial;

		private static readonly object Lock = new object();
	}
}
