﻿using System;
using System.IO;
using Mirror;

public static class OffsetSerializer
{
	public unsafe static void WriteOffset(this NetworkWriter writer, Offset value)
	{
		writer.EnsureLength(sizeof(Offset));
		fixed (byte* ptr = &writer.buffer[writer.Position])
		{
			*(Offset*)ptr = value;
		}
		writer.Position += sizeof(Offset);
	}

	public unsafe static Offset ReadOffset(this NetworkReader reader)
	{
		if (reader.Position + sizeof(Offset) > reader.buffer.Count)
		{
			throw new EndOfStreamException("ReadByte out of range:" + ((reader != null) ? reader.ToString() : null));
		}
		Offset offset;
		fixed (byte* ptr = &reader.buffer.Array[reader.buffer.Offset + reader.Position])
		{
			offset = *(Offset*)ptr;
		}
		reader.Position += sizeof(Offset);
		return offset;
	}
}
