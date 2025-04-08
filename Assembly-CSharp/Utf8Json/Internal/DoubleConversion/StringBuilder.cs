using System;

namespace Utf8Json.Internal.DoubleConversion
{
	internal struct StringBuilder
	{
		public StringBuilder(byte[] buffer, int position)
		{
			this.buffer = buffer;
			this.offset = position;
		}

		public void AddCharacter(byte str)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, 1);
			byte[] array = this.buffer;
			int num = this.offset;
			this.offset = num + 1;
			array[num] = str;
		}

		public void AddString(byte[] str)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, str.Length);
			for (int i = 0; i < str.Length; i++)
			{
				this.buffer[this.offset + i] = str[i];
			}
			this.offset += str.Length;
		}

		public void AddSubstring(byte[] str, int length)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, length);
			for (int i = 0; i < length; i++)
			{
				this.buffer[this.offset + i] = str[i];
			}
			this.offset += length;
		}

		public void AddSubstring(byte[] str, int start, int length)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, length);
			for (int i = 0; i < length; i++)
			{
				this.buffer[this.offset + i] = str[start + i];
			}
			this.offset += length;
		}

		public void AddPadding(byte c, int count)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, count);
			for (int i = 0; i < count; i++)
			{
				this.buffer[this.offset + i] = c;
			}
			this.offset += count;
		}

		public void AddStringSlow(string str)
		{
			BinaryUtil.EnsureCapacity(ref this.buffer, this.offset, StringEncoding.UTF8.GetMaxByteCount(str.Length));
			this.offset += StringEncoding.UTF8.GetBytes(str, 0, str.Length, this.buffer, this.offset);
		}

		public byte[] buffer;

		public int offset;
	}
}
