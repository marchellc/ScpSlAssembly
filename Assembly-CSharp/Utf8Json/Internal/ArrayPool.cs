using System;

namespace Utf8Json.Internal
{
	internal class ArrayPool<T>
	{
		public ArrayPool(int bufferLength)
		{
			this.bufferLength = bufferLength;
			this.buffers = new T[4][];
			this.gate = new object();
		}

		public T[] Rent()
		{
			object obj = this.gate;
			T[] array2;
			lock (obj)
			{
				if (this.index >= this.buffers.Length)
				{
					Array.Resize<T[]>(ref this.buffers, this.buffers.Length * 2);
				}
				if (this.buffers[this.index] == null)
				{
					this.buffers[this.index] = new T[this.bufferLength];
				}
				T[] array = this.buffers[this.index];
				this.buffers[this.index] = null;
				this.index++;
				array2 = array;
			}
			return array2;
		}

		public void Return(T[] array)
		{
			if (array.Length != this.bufferLength)
			{
				throw new InvalidOperationException("return buffer is not from pool");
			}
			object obj = this.gate;
			lock (obj)
			{
				if (this.index != 0)
				{
					T[][] array2 = this.buffers;
					int num = this.index - 1;
					this.index = num;
					array2[num] = array;
				}
			}
		}

		private readonly int bufferLength;

		private readonly object gate;

		private int index;

		private T[][] buffers;
	}
}
