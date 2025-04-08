using System;
using System.Collections.Generic;
using Hints;
using Mirror;

namespace Utils.Networking
{
	public static class HintEffectArrayReaderWriter
	{
		public static HintEffect[] ReadHintEffectArray(this NetworkReader reader)
		{
			return ArrayReaderWriter<HintEffect>.ReadArray(reader, new ArrayReaderWriter<HintEffect>.ReadItem(HintEffectReaderWriter.ReadHintEffect));
		}

		public static void WriteHintEffectArray(this NetworkWriter writer, IReadOnlyCollection<HintEffect> array)
		{
			ArrayReaderWriter<HintEffect>.WriteArray(writer, array, new ArrayReaderWriter<HintEffect>.WriteItem(HintEffectReaderWriter.WriteHintEffect));
		}
	}
}
