using System;
using System.Threading;

namespace Utf8Json.Internal;

internal class ThreadsafeTypeKeyHashTable<TValue>
{
	private class Entry
	{
		public Type Key;

		public TValue Value;

		public int Hash;

		public Entry Next;

		public override string ToString()
		{
			return this.Key?.ToString() + "(" + this.Count() + ")";
		}

		private int Count()
		{
			int num = 1;
			Entry entry = this;
			while (entry.Next != null)
			{
				num++;
				entry = entry.Next;
			}
			return num;
		}
	}

	private Entry[] buckets;

	private int size;

	private readonly object writerLock = new object();

	private readonly float loadFactor;

	public ThreadsafeTypeKeyHashTable(int capacity = 4, float loadFactor = 0.75f)
	{
		int num = ThreadsafeTypeKeyHashTable<TValue>.CalculateCapacity(capacity, loadFactor);
		this.buckets = new Entry[num];
		this.loadFactor = loadFactor;
	}

	public bool TryAdd(Type key, TValue value)
	{
		return this.TryAdd(key, (Type _) => value);
	}

	public bool TryAdd(Type key, Func<Type, TValue> valueFactory)
	{
		TValue resultingValue;
		return this.TryAddInternal(key, valueFactory, out resultingValue);
	}

	private bool TryAddInternal(Type key, Func<Type, TValue> valueFactory, out TValue resultingValue)
	{
		lock (this.writerLock)
		{
			int num = ThreadsafeTypeKeyHashTable<TValue>.CalculateCapacity(this.size + 1, this.loadFactor);
			if (this.buckets.Length < num)
			{
				Entry[] value = new Entry[num];
				for (int i = 0; i < this.buckets.Length; i++)
				{
					for (Entry entry = this.buckets[i]; entry != null; entry = entry.Next)
					{
						Entry newEntryOrNull = new Entry
						{
							Key = entry.Key,
							Value = entry.Value,
							Hash = entry.Hash
						};
						this.AddToBuckets(value, key, newEntryOrNull, null, out resultingValue);
					}
				}
				bool num2 = this.AddToBuckets(value, key, null, valueFactory, out resultingValue);
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref this.buckets, value);
				if (num2)
				{
					this.size++;
				}
				return num2;
			}
			bool num3 = this.AddToBuckets(this.buckets, key, null, valueFactory, out resultingValue);
			if (num3)
			{
				this.size++;
			}
			return num3;
		}
	}

	private bool AddToBuckets(Entry[] buckets, Type newKey, Entry newEntryOrNull, Func<Type, TValue> valueFactory, out TValue resultingValue)
	{
		int num = newEntryOrNull?.Hash ?? newKey.GetHashCode();
		if (buckets[num & (buckets.Length - 1)] == null)
		{
			if (newEntryOrNull != null)
			{
				resultingValue = newEntryOrNull.Value;
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref buckets[num & (buckets.Length - 1)], newEntryOrNull);
			}
			else
			{
				resultingValue = valueFactory(newKey);
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref buckets[num & (buckets.Length - 1)], new Entry
				{
					Key = newKey,
					Value = resultingValue,
					Hash = num
				});
			}
		}
		else
		{
			Entry entry = buckets[num & (buckets.Length - 1)];
			while (true)
			{
				if (entry.Key == newKey)
				{
					resultingValue = entry.Value;
					return false;
				}
				if (entry.Next == null)
				{
					break;
				}
				entry = entry.Next;
			}
			if (newEntryOrNull != null)
			{
				resultingValue = newEntryOrNull.Value;
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref entry.Next, newEntryOrNull);
			}
			else
			{
				resultingValue = valueFactory(newKey);
				ThreadsafeTypeKeyHashTable<TValue>.VolatileWrite(ref entry.Next, new Entry
				{
					Key = newKey,
					Value = resultingValue,
					Hash = num
				});
			}
		}
		return true;
	}

	public bool TryGetValue(Type key, out TValue value)
	{
		Entry[] array = this.buckets;
		int hashCode = key.GetHashCode();
		Entry entry = array[hashCode & (array.Length - 1)];
		if (entry != null)
		{
			if (entry.Key == key)
			{
				value = entry.Value;
				return true;
			}
			for (Entry next = entry.Next; next != null; next = next.Next)
			{
				if (next.Key == key)
				{
					value = next.Value;
					return true;
				}
			}
		}
		value = default(TValue);
		return false;
	}

	public TValue GetOrAdd(Type key, Func<Type, TValue> valueFactory)
	{
		if (this.TryGetValue(key, out var value))
		{
			return value;
		}
		this.TryAddInternal(key, valueFactory, out value);
		return value;
	}

	private static int CalculateCapacity(int collectionSize, float loadFactor)
	{
		int num = (int)((float)collectionSize / loadFactor);
		int num2;
		for (num2 = 1; num2 < num; num2 <<= 1)
		{
		}
		if (num2 < 8)
		{
			return 8;
		}
		return num2;
	}

	private static void VolatileWrite(ref Entry location, Entry value)
	{
		Thread.MemoryBarrier();
		location = value;
	}

	private static void VolatileWrite(ref Entry[] location, Entry[] value)
	{
		Thread.MemoryBarrier();
		location = value;
	}
}
