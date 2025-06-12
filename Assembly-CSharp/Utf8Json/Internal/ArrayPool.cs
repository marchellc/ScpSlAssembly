using System;

namespace Utf8Json.Internal;

internal class ArrayPool<T>
{
	private readonly int bufferLength;

	private readonly object gate;

	private int index;

	private T[][] buffers;

	public ArrayPool(int bufferLength)
	{
		this.bufferLength = bufferLength;
		this.buffers = new T[4][];
		this.gate = new object();
	}

	public T[] Rent()
	{
		lock (this.gate)
		{
			if (this.index >= this.buffers.Length)
			{
				Array.Resize(ref this.buffers, this.buffers.Length * 2);
			}
			if (this.buffers[this.index] == null)
			{
				this.buffers[this.index] = new T[this.bufferLength];
			}
			T[] result = this.buffers[this.index];
			this.buffers[this.index] = null;
			this.index++;
			return result;
		}
	}

	public void Return(T[] array)
	{
		if (array.Length != this.bufferLength)
		{
			throw new InvalidOperationException("return buffer is not from pool");
		}
		lock (this.gate)
		{
			if (this.index != 0)
			{
				this.buffers[--this.index] = array;
			}
		}
	}
}
