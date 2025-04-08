using System;
using System.Collections.Generic;
using Mirror;

namespace Utils.Networking
{
	public static class ArrayReaderWriter<TItem>
	{
		public static TItem[] ReadArray(NetworkReader reader, ArrayReaderWriter<TItem>.ReadItem itemReader)
		{
			int num = reader.ReadInt();
			if (num < 0)
			{
				return null;
			}
			TItem[] array = new TItem[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = itemReader(reader);
			}
			return array;
		}

		public static void WriteArray(NetworkWriter writer, IReadOnlyCollection<TItem> array, ArrayReaderWriter<TItem>.WriteItem itemWriter)
		{
			if (array != null)
			{
				writer.WriteInt(array.Count);
				using (IEnumerator<TItem> enumerator = array.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						TItem titem = enumerator.Current;
						itemWriter(writer, titem);
					}
					return;
				}
			}
			writer.WriteInt(-1);
		}

		public delegate TItem ReadItem(NetworkReader reader);

		public delegate void WriteItem(NetworkWriter writer, TItem item);
	}
}
