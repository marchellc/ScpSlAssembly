using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal
{
	internal class ILStreamReader : BinaryReader
	{
		public int CurrentPosition
		{
			get
			{
				return (int)this.BaseStream.Position;
			}
		}

		public bool EndOfStream
		{
			get
			{
				return (int)this.BaseStream.Position >= this.endPosition;
			}
		}

		static ILStreamReader()
		{
			FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int i = 0; i < fields.Length; i++)
			{
				OpCode opCode = (OpCode)fields[i].GetValue(null);
				ushort num = (ushort)opCode.Value;
				if (num < 256)
				{
					ILStreamReader.oneByteOpCodes[(int)num] = opCode;
				}
				else if ((num & 65280) == 65024)
				{
					ILStreamReader.twoByteOpCodes[(int)(num & 255)] = opCode;
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
				return ILStreamReader.oneByteOpCodes[(int)b];
			}
			b = this.ReadByte();
			return ILStreamReader.twoByteOpCodes[(int)b];
		}

		public int ReadMetadataToken()
		{
			return this.ReadInt32();
		}

		private static readonly OpCode[] oneByteOpCodes = new OpCode[256];

		private static readonly OpCode[] twoByteOpCodes = new OpCode[256];

		private int endPosition;
	}
}
