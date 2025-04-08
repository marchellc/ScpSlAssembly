using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public class ArrayFormatter<T> : IJsonFormatter<T[]>, IJsonFormatter, IOverwriteJsonFormatter<T[]>
	{
		public ArrayFormatter()
			: this(CollectionDeserializeToBehaviour.Add)
		{
		}

		public ArrayFormatter(CollectionDeserializeToBehaviour deserializeToBehaviour)
		{
			this.deserializeToBehaviour = deserializeToBehaviour;
		}

		public void Serialize(ref JsonWriter writer, T[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			if (value.Length != 0)
			{
				formatterWithVerify.Serialize(ref writer, value[0], formatterResolver);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				formatterWithVerify.Serialize(ref writer, value[i], formatterResolver);
			}
			writer.WriteEndArray();
		}

		public T[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			int num = 0;
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			T[] array = ArrayFormatter<T>.arrayPool.Rent();
			T[] array4;
			try
			{
				T[] array2 = array;
				reader.ReadIsBeginArrayWithVerify();
				while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
				{
					if (array2.Length < num)
					{
						Array.Resize<T>(ref array2, array2.Length * 2);
					}
					array2[num - 1] = formatterWithVerify.Deserialize(ref reader, formatterResolver);
				}
				T[] array3 = new T[num];
				Array.Copy(array2, array3, num);
				Array.Clear(array, 0, Math.Min(num, array.Length));
				array4 = array3;
			}
			finally
			{
				ArrayFormatter<T>.arrayPool.Return(array);
			}
			return array4;
		}

		public void DeserializeTo(ref T[] value, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return;
			}
			int num = 0;
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			if (this.deserializeToBehaviour == CollectionDeserializeToBehaviour.Add)
			{
				T[] array = ArrayFormatter<T>.arrayPool.Rent();
				try
				{
					T[] array2 = array;
					reader.ReadIsBeginArrayWithVerify();
					while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
					{
						if (array2.Length < num)
						{
							Array.Resize<T>(ref array2, array2.Length * 2);
						}
						array2[num - 1] = formatterWithVerify.Deserialize(ref reader, formatterResolver);
					}
					if (num == 0)
					{
						return;
					}
					T[] array3 = new T[value.Length + num];
					Array.Copy(value, 0, array3, 0, value.Length);
					Array.Copy(array2, 0, array3, value.Length, num);
					Array.Clear(array, 0, Math.Min(num, array.Length));
					return;
				}
				finally
				{
					ArrayFormatter<T>.arrayPool.Return(array);
				}
			}
			T[] array4 = value;
			reader.ReadIsBeginArrayWithVerify();
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array4.Length < num)
				{
					Array.Resize<T>(ref array4, array4.Length * 2);
				}
				array4[num - 1] = formatterWithVerify.Deserialize(ref reader, formatterResolver);
			}
			Array.Resize<T>(ref array4, num);
		}

		private static readonly ArrayPool<T> arrayPool = new ArrayPool<T>(99);

		private readonly CollectionDeserializeToBehaviour deserializeToBehaviour;
	}
}
