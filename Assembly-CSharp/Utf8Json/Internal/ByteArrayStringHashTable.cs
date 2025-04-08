using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Utf8Json.Internal
{
	internal class ByteArrayStringHashTable<T> : IEnumerable<KeyValuePair<string, T>>, IEnumerable
	{
		public ByteArrayStringHashTable(int capacity)
			: this(capacity, 0.42f)
		{
		}

		public ByteArrayStringHashTable(int capacity, float loadFactor)
		{
			int num = ByteArrayStringHashTable<T>.CalculateCapacity(capacity, loadFactor);
			this.buckets = new ByteArrayStringHashTable<T>.Entry[num][];
			this.indexFor = (ulong)((long)this.buckets.Length - 1L);
		}

		public void Add(string key, T value)
		{
			if (!this.TryAddInternal(Encoding.UTF8.GetBytes(key), value))
			{
				throw new ArgumentException("Key was already exists. Key:" + key);
			}
		}

		public void Add(byte[] key, T value)
		{
			if (!this.TryAddInternal(key, value))
			{
				throw new ArgumentException("Key was already exists. Key:" + ((key != null) ? key.ToString() : null));
			}
		}

		private bool TryAddInternal(byte[] key, T value)
		{
			ulong num = ByteArrayStringHashTable<T>.ByteArrayGetHashCode(key, 0, key.Length);
			ByteArrayStringHashTable<T>.Entry entry = new ByteArrayStringHashTable<T>.Entry
			{
				Key = key,
				Value = value
			};
			checked
			{
				ByteArrayStringHashTable<T>.Entry[] array = this.buckets[(int)((IntPtr)(num & this.indexFor))];
				if (array == null)
				{
					this.buckets[(int)((IntPtr)(num & this.indexFor))] = new ByteArrayStringHashTable<T>.Entry[] { entry };
				}
				else
				{
					unchecked
					{
						for (int i = 0; i < array.Length; i++)
						{
							byte[] key2 = array[i].Key;
							if (ByteArrayComparer.Equals(key, 0, key.Length, key2))
							{
								return false;
							}
						}
						ByteArrayStringHashTable<T>.Entry[] array2 = new ByteArrayStringHashTable<T>.Entry[array.Length + 1];
						Array.Copy(array, array2, array.Length);
						array = array2;
						array[array.Length - 1] = entry;
					}
					this.buckets[(int)((IntPtr)(num & this.indexFor))] = array;
				}
				return true;
			}
		}

		public bool TryGetValue(ArraySegment<byte> key, out T value)
		{
			ByteArrayStringHashTable<T>.Entry[][] array = this.buckets;
			ulong num = ByteArrayStringHashTable<T>.ByteArrayGetHashCode(key.Array, key.Offset, key.Count);
			ByteArrayStringHashTable<T>.Entry[] array2 = array[(int)(checked((IntPtr)(num & this.indexFor)))];
			if (array2 != null)
			{
				ByteArrayStringHashTable<T>.Entry entry = array2[0];
				if (ByteArrayComparer.Equals(key.Array, key.Offset, key.Count, entry.Key))
				{
					value = entry.Value;
					return true;
				}
				for (int i = 1; i < array2.Length; i++)
				{
					ByteArrayStringHashTable<T>.Entry entry2 = array2[i];
					if (ByteArrayComparer.Equals(key.Array, key.Offset, key.Count, entry2.Key))
					{
						value = entry2.Value;
						return true;
					}
				}
			}
			value = default(T);
			return false;
		}

		private static ulong ByteArrayGetHashCode(byte[] x, int offset, int count)
		{
			uint num = 0U;
			if (x != null)
			{
				int num2 = offset + count;
				num = 2166136261U;
				for (int i = offset; i < num2; i++)
				{
					num = ((uint)x[i] ^ num) * 16777619U;
				}
			}
			return (ulong)num;
		}

		private static int CalculateCapacity(int collectionSize, float loadFactor)
		{
			int num = (int)((float)collectionSize / loadFactor);
			int i;
			for (i = 1; i < num; i <<= 1)
			{
			}
			if (i < 8)
			{
				return 8;
			}
			return i;
		}

		public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
		{
			ByteArrayStringHashTable<T>.Entry[][] array = this.buckets;
			foreach (ByteArrayStringHashTable<T>.Entry[] array3 in array)
			{
				if (array3 != null)
				{
					foreach (ByteArrayStringHashTable<T>.Entry entry in array3)
					{
						yield return new KeyValuePair<string, T>(Encoding.UTF8.GetString(entry.Key), entry.Value);
					}
					ByteArrayStringHashTable<T>.Entry[] array4 = null;
				}
			}
			ByteArrayStringHashTable<T>.Entry[][] array2 = null;
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private readonly ByteArrayStringHashTable<T>.Entry[][] buckets;

		private readonly ulong indexFor;

		private struct Entry
		{
			public override string ToString()
			{
				string[] array = new string[5];
				array[0] = "(";
				array[1] = Encoding.UTF8.GetString(this.Key);
				array[2] = ", ";
				int num = 3;
				T value = this.Value;
				array[num] = ((value != null) ? value.ToString() : null);
				array[4] = ")";
				return string.Concat(array);
			}

			public byte[] Key;

			public T Value;
		}
	}
}
