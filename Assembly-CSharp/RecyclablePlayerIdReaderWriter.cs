using System;
using Mirror;

public static class RecyclablePlayerIdReaderWriter
{
	public static void WriteRecyclablePlayerId(this NetworkWriter writer, RecyclablePlayerId val)
	{
		for (int i = val.Value; i >= 0; i -= 255)
		{
			writer.WriteByte((byte)Math.Min(i, 255));
		}
	}

	public static RecyclablePlayerId ReadRecyclablePlayerId(this NetworkReader reader)
	{
		int num = 0;
		byte b;
		do
		{
			b = reader.ReadByte();
			num += (int)b;
		}
		while (b == 255);
		return new RecyclablePlayerId(num);
	}
}
