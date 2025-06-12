using System;

namespace Utf8Json.Internal;

public struct ArrayBuffer<T>
{
	public T[] Buffer;

	public int Size;

	public ArrayBuffer(int initialSize)
	{
		this.Buffer = new T[initialSize];
		this.Size = 0;
	}

	public void Add(T value)
	{
		if (this.Size >= this.Buffer.Length)
		{
			Array.Resize(ref this.Buffer, this.Size * 2);
		}
		this.Buffer[this.Size++] = value;
	}

	public T[] ToArray()
	{
		if (this.Buffer.Length == this.Size)
		{
			return this.Buffer;
		}
		T[] array = new T[this.Size];
		Array.Copy(this.Buffer, array, this.Size);
		return array;
	}
}
