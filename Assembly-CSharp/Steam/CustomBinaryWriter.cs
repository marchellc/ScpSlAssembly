using System;
using System.IO;
using System.Text;

namespace Steam
{
	public class CustomBinaryWriter : IDisposable
	{
		public CustomBinaryWriter()
		{
			this.stream = new MemoryStream();
			this.writer = new BinaryWriter(this.stream);
		}

		public void WriteByte(byte val)
		{
			this.writer.Write(val);
		}

		public void WriteShort(short val)
		{
			this.writer.Write(val);
		}

		public void WriteInt(int val)
		{
			this.writer.Write(val);
		}

		public void WriteFloat(float val)
		{
			this.writer.Write(val);
		}

		public void WriteLong(long val)
		{
			this.writer.Write(val);
		}

		public void WriteString(string val)
		{
			this.writer.Write(Encoding.UTF8.GetBytes(val));
			this.writer.Write(0);
		}

		public byte[] ToArray()
		{
			return this.stream.ToArray();
		}

		public void Dispose()
		{
			this.writer.Dispose();
			this.stream.Dispose();
		}

		private MemoryStream stream;

		private BinaryWriter writer;
	}
}
