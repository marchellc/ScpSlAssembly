namespace PlayerRoles.Ragdolls;

public static class RagdollSerialGenerator
{
	private static ushort _lastGenerated = 0;

	private static readonly object Lock = new object();

	public static ushort GenerateNext()
	{
		lock (RagdollSerialGenerator.Lock)
		{
			int num = RagdollSerialGenerator._lastGenerated + 1;
			if (num > 65535)
			{
				num = 1;
			}
			RagdollSerialGenerator._lastGenerated = (ushort)num;
			return RagdollSerialGenerator._lastGenerated;
		}
	}
}
