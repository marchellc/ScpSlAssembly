using System;
using System.Threading;

namespace Utf8Json.Internal
{
	internal class ThreadsafeTypeKeyHashTable<TValue>
	{
		public ThreadsafeTypeKeyHashTable(int capacity = 4, float loadFactor = 0.75f)
		{
			int num = ThreadsafeTypeKeyHashTable<TValue>.CalculateCapacity(capacity, loadFactor);
			this.buckets = new ThreadsafeTypeKeyHashTable<TValue>.Entry[num];
			this.loadFactor = loadFactor;
		}

		public bool TryAdd(Type key, TValue value)
		{
			return this.TryAdd(key, (Type _) => value);
		}

		public bool TryAdd(Type key, Func<Type, TValue> valueFactory)
		{
			TValue tvalue;
			return this.TryAddInternal(key, valueFactory, out tvalue);
		}

		private bool TryAddInternal(Type key, Func<Type, TValue> valueFactory, out TValue resultingValue)
		{
			object obj = this.writerLock;
			bool flag3;
			lock (obj)
			{
				int num = ThreadsafeTypeKeyHashTable<TValue>.CalculateCapacity(this.size + 1, this.loadFactor);
				if (this.buckets.Length < num)
				{
					ThreadsafeTypeKeyHashTable<TValue>.Entry[] array = new ThreadsafeTypeKeyHashTable<TValue>.Entry[num];
					for (int i = 0; i < this.buckets.Length; i++)
					{
						for (ThreadsafeTypeKeyHashTable<TValue>.Entry entry = this.buckets[i]; entry != null; entry = entry.Next)
						{
							ThreadsafeTypeKeyHashTable<TValue>.Entry entry2 = new ThreadsafeTypeKeyHashTable<TValue>.Entry
							{
								Key = entry.Key,
								Value = entry.Value,
								Hash = entry.Hash
							};
							this.AddToBuckets(array, key, entry2, null, out resultingValue);
						}
					}
					bool flag2 = this.AddToBuckets(array, key, null, valueFactory, out resultingValue);
					ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref this.buckets, array);
					if (flag2)
					{
						this.size++;
					}
					flag3 = flag2;
				}
				else
				{
					bool flag4 = this.AddToBuckets(this.buckets, key, null, valueFactory, out resultingValue);
					if (flag4)
					{
						this.size++;
					}
					flag3 = flag4;
				}
			}
			return flag3;
		}

		private bool AddToBuckets(ThreadsafeTypeKeyHashTable<TValue>.Entry[] buckets, Type newKey, ThreadsafeTypeKeyHashTable<TValue>.Entry newEntryOrNull, Func<Type, TValue> valueFactory, out TValue resultingValue)
		{
			int num = ((newEntryOrNull != null) ? newEntryOrNull.Hash : newKey.GetHashCode());
			if (buckets[num & (buckets.Length - 1)] != null)
			{
				ThreadsafeTypeKeyHashTable<TValue>.Entry entry = buckets[num & (buckets.Length - 1)];
				while (!(entry.Key == newKey))
				{
					if (entry.Next == null)
					{
						if (newEntryOrNull != null)
						{
							resultingValue = newEntryOrNull.Value;
							ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref entry.Next, newEntryOrNull);
							return true;
						}
						resultingValue = valueFactory(newKey);
						ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref entry.Next, new ThreadsafeTypeKeyHashTable<TValue>.Entry
						{
							Key = newKey,
							Value = resultingValue,
							Hash = num
						});
						return true;
					}
					else
					{
						entry = entry.Next;
					}
				}
				resultingValue = entry.Value;
				return false;
			}
			if (newEntryOrNull != null)
			{
				resultingValue = newEntryOrNull.Value;
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref buckets[num & (buckets.Length - 1)], newEntryOrNull);
			}
			else
			{
				resultingValue = valueFactory(newKey);
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref buckets[num & (buckets.Length - 1)], new ThreadsafeTypeKeyHashTable<TValue>.Entry
				{
					Key = newKey,
					Value = resultingValue,
					Hash = num
				});
			}
			return true;
		}

		public bool TryGetValue(Type key, out TValue value)
		{
			ThreadsafeTypeKeyHashTable<TValue>.Entry[] array = this.buckets;
			int hashCode = key.GetHashCode();
			ThreadsafeTypeKeyHashTable<TValue>.Entry entry = array[hashCode & (array.Length - 1)];
			if (entry != null)
			{
				if (entry.Key == key)
				{
					value = entry.Value;
					return true;
				}
				for (ThreadsafeTypeKeyHashTable<TValue>.Entry entry2 = entry.Next; entry2 != null; entry2 = entry2.Next)
				{
					if (entry2.Key == key)
					{
						value = entry2.Value;
						return true;
					}
				}
			}
			value = default(TValue);
			return false;
		}

		public TValue GetOrAdd(Type key, Func<Type, TValue> valueFactory)
		{
			TValue tvalue;
			if (this.TryGetValue(key, out tvalue))
			{
				return tvalue;
			}
			this.TryAddInternal(key, valueFactory, out tvalue);
			return tvalue;
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

		private static void VolatileWrite(ref ThreadsafeTypeKeyHashTable<TValue>.Entry location, ThreadsafeTypeKeyHashTable<TValue>.Entry value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		private static void VolatileWrite(ref ThreadsafeTypeKeyHashTable<TValue>.Entry[] location, ThreadsafeTypeKeyHashTable<TValue>.Entry[] value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		private ThreadsafeTypeKeyHashTable<TValue>.Entry[] buckets;

		private int size;

		private readonly object writerLock = new object();

		private readonly float loadFactor;

		private class Entry
		{
			public override string ToString()
			{
				Type key = this.Key;
				return ((key != null) ? key.ToString() : null) + "(" + this.Count().ToString() + ")";
			}

			private int Count()
			{
				int num = 1;
				ThreadsafeTypeKeyHashTable<TValue>.Entry entry = this;
				while (entry.Next != null)
				{
					num++;
					entry = entry.Next;
				}
				return num;
			}

			public Type Key;

			public TValue Value;

			public int Hash;

			public ThreadsafeTypeKeyHashTable<TValue>.Entry Next;
		}
	}
}
