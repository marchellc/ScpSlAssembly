using System;
using System.IO;
using Mirror;

public static class LowPrecisionQuaternionSerializer
{
	public unsafe static void WriteLowPrecisionQuaternion(this NetworkWriter writer, LowPrecisionQuaternion value)
	{
		writer.EnsureLength(sizeof(LowPrecisionQuaternion));
		fixed (byte* ptr = &writer.buffer[writer.Position])
		{
			*(LowPrecisionQuaternion*)ptr = value;
		}
		writer.Position += sizeof(LowPrecisionQuaternion);
	}

	public unsafe static LowPrecisionQuaternion ReadLowPrecisionQuaternion(this NetworkReader reader)
	{
		if (reader.Position + sizeof(LowPrecisionQuaternion) > reader.buffer.Count)
		{
			throw new EndOfStreamException("ReadByte out of range:" + ((reader != null) ? reader.ToString() : null));
		}
		LowPrecisionQuaternion lowPrecisionQuaternion;
		fixed (byte* ptr = &reader.buffer.Array[reader.buffer.Offset + reader.Position])
		{
			lowPrecisionQuaternion = *(LowPrecisionQuaternion*)ptr;
		}
		reader.Position += sizeof(LowPrecisionQuaternion);
		return lowPrecisionQuaternion;
	}
}
