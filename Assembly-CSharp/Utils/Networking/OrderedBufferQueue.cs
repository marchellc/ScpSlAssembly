using System;
using System.Collections.Generic;

namespace Utils.Networking
{
	public class OrderedBufferQueue<T>
	{
		public int Count
		{
			get
			{
				return (int)(this._newestItem - this._oldestItem + 1L);
			}
		}

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
			data = this._buffer[this.GetBufferIndex(this._oldestItem + 1L)];
			if (this._oldestItem >= this._newestItem)
			{
				return false;
			}
			this._oldestItem += 1L;
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
				int bufferIndex = this.GetBufferIndex(this._newestItem - (long)i);
				T t = this._buffer[bufferIndex];
				if (!this._isNewer(t, itemToAdd))
				{
					this._newestItem -= (long)(i - 1);
					while (i-- > 0)
					{
						this.AddToBuffer(this._reorganizer[i]);
					}
					this._reorganizer.Clear();
					return;
				}
				this._buffer[bufferIndex] = itemToAdd;
				this._reorganizer.Add(t);
			}
		}

		private void EnsureCapacity()
		{
			if (this.Count != this._capacity)
			{
				return;
			}
			int bufferIndex = this.GetBufferIndex(this._oldestItem);
			int num = this._capacity * 2;
			if (bufferIndex == 0)
			{
				Array.Resize<T>(ref this._buffer, num);
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

		private void AddToBuffer(T nb)
		{
			this.EnsureCapacity();
			T[] buffer = this._buffer;
			long num = this._newestItem + 1L;
			this._newestItem = num;
			buffer[this.GetBufferIndex(num)] = nb;
		}

		private int GetBufferIndex(long position)
		{
			return (int)(position % (long)this._capacity + (long)this._capacity) % this._capacity;
		}

		private T _newestSoFar;

		private long _newestItem;

		private long _oldestItem;

		private T[] _buffer;

		private int _capacity;

		private readonly List<T> _reorganizer;

		private readonly Func<T, T, bool> _isNewer;
	}
}
