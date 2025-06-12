using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal;

internal class ILStreamReader : BinaryReader
{
	private static readonly OpCode[] oneByteOpCodes;

	private static readonly OpCode[] twoByteOpCodes;

	private int endPosition;

	public int CurrentPosition => (int)this.BaseStream.Position;

	public bool EndOfStream => (int)this.BaseStream.Position >= this.endPosition;

	static ILStreamReader()
	{
		ILStreamReader.oneByteOpCodes = new OpCode[256];
		ILStreamReader.twoByteOpCodes = new OpCode[256];
		FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
		for (int i = 0; i < fields.Length; i++)
		{
			OpCode opCode = (OpCode)fields[i].GetValue(null);
			ushort num = (ushort)opCode.Value;
			if (num < 256)
			{
				ILStreamReader.oneByteOpCodes[num] = opCode;
			}
			else if ((num & 0xFF00) == 65024)
			{
				ILStreamReader.twoByteOpCodes[num & 0xFF] = opCode;
			}
		}
	}

	public ILStreamReader(byte[] ilByteArray)
		: base(new MemoryStream(ilByteArray))
	{
		this.endPosition = ilByteArray.Length;
	}

	public OpCode ReadOpCode()
	{
		byte b = this.ReadByte();
		if (b != 254)
		{
			return ILStreamReader.oneByteOpCodes[b];
		}
		b = this.ReadByte();
		return ILStreamReader.twoByteOpCodes[b];
	}

	public int ReadMetadataToken()
	{
		return this.ReadInt32();
	}
}
