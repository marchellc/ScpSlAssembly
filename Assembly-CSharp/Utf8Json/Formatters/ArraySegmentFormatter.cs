using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public class ArraySegmentFormatter<T> : IJsonFormatter<ArraySegment<T>>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, ArraySegment<T> value, IJsonFormatterResolver formatterResolver)
		{
			if (value.Array == null)
			{
				writer.WriteNull();
				return;
			}
			T[] array = value.Array;
			int offset = value.Offset;
			int count = value.Count;
			writer.WriteBeginArray();
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			if (count != 0)
			{
				formatterWithVerify.Serialize(ref writer, value.Array[offset], formatterResolver);
			}
			for (int i = 1; i < count; i++)
			{
				writer.WriteValueSeparator();
				formatterWithVerify.Serialize(ref writer, array[offset + i], formatterResolver);
			}
			writer.WriteEndArray();
		}

		public ArraySegment<T> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<T> arraySegment;
			if (reader.ReadIsNull())
			{
				arraySegment = default(ArraySegment<T>);
				return arraySegment;
			}
			int num = 0;
			IJsonFormatter<T> formatterWithVerify = formatterResolver.GetFormatterWithVerify<T>();
			T[] array = ArraySegmentFormatter<T>.arrayPool.Rent();
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
				arraySegment = new ArraySegment<T>(array3, 0, array3.Length);
			}
			finally
			{
				ArraySegmentFormatter<T>.arrayPool.Return(array);
			}
			return arraySegment;
		}

		private static readonly ArrayPool<T> arrayPool = new ArrayPool<T>(99);
	}
}
