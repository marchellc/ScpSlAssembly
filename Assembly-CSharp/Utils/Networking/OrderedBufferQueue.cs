using System;
using System.Collections.Generic;

namespace Utils.Networking;

public class OrderedBufferQueue<T>
{
	private T _newestSoFar;

	private long _newestItem;

	private long _oldestItem;

	private T[] _buffer;

	private int _capacity;

	private readonly List<T> _reorganizer;

	private readonly Func<T, T, bool> _isNewer;

	public int Count => (int)(this._newestItem - this._oldestItem + 1);

	public OrderedBufferQueue(Func<T, T, bool> isNewerComparer)
	{
		this._reorganizer = new List<T>();
		this._capacity = 64;
		this._buffer = new T[64];
		this._newestItem = -1L;
		this._oldestItem = -1L;
		this._isNewer = isNewerComparer;
	}

	public bool TryDequeue(out T data)
	{
		data = this._buffer[this.GetBufferIndex(this._oldestItem + 1)];
		if (this._oldestItem >= this._newestItem)
		{
			return false;
		}
		this._oldestItem++;
		return true;
	}

	public void Enqueue(T itemToAdd)
	{
		if (this._isNewer(itemToAdd, this._newestSoFar))
		{
			this.AddToBuffer(itemToAdd);
			this._newestSoFar = itemToAdd;
			return;
		}
		for (int i = 0; i < this._capacity; i++)
		{
			int bufferIndex = this.GetBufferIndex(this._newestItem - i);
			T val = this._buffer[bufferIndex];
			if (this._isNewer(val, itemToAdd))
			{
				this._buffer[bufferIndex] = itemToAdd;
				this._reorganizer.Add(val);
				continue;
			}
			this._newestItem -= i - 1;
			while (i-- > 0)
			{
				this.AddToBuffer(this._reorganizer[i]);
			}
			this._reorganizer.Clear();
			break;
		}
	}

	private void EnsureCapacity()
	{
		if (this.Count == this._capacity)
		{
			int bufferIndex = this.GetBufferIndex(this._oldestItem);
			int num = this._capacity * 2;
			if (bufferIndex == 0)
			{
				Array.Resize(ref this._buffer, num);
			}
			else
			{
				T[] array = new T[num];
				Array.Copy(this._buffer, bufferIndex, array, 0, this._capacity - bufferIndex);
				Array.Copy(this._buffer, 0, array, this._capacity - bufferIndex, bufferIndex);
				this._buffer = array;
			}
			this._newestItem -= this._oldestItem;
			this._oldestItem = 0L;
			this._capacity = this._buffer.Length;
		}
	}

	private void AddToBuffer(T nb)
	{
		this.EnsureCapacity();
		this._buffer[this.GetBufferIndex(++this._newestItem)] = nb;
	}

	private int GetBufferIndex(long position)
	{
		return (int)(position % this._capacity + this._capacity) % this._capacity;
	}
}
