using System;
using Mirror;

namespace Utils.Networking
{
	public static class NullableBoolReaderWriter
	{
		public static void WriteNullableBool(this NetworkWriter writer, bool? val)
		{
			NullableBoolReaderWriter.NullableBoolValue nullableBoolValue = NullableBoolReaderWriter.NullableBoolValue.Null;
			if (val != null)
			{
				nullableBoolValue = (val.Value ? NullableBoolReaderWriter.NullableBoolValue.True : NullableBoolReaderWriter.NullableBoolValue.False);
			}
			writer.WriteByte((byte)nullableBoolValue);
		}

		public static bool? ReadNullableBool(this NetworkReader reader)
		{
			NullableBoolReaderWriter.NullableBoolValue nullableBoolValue = (NullableBoolReaderWriter.NullableBoolValue)reader.ReadByte();
			if (nullableBoolValue != NullableBoolReaderWriter.NullableBoolValue.Null)
			{
				return new bool?(nullableBoolValue == NullableBoolReaderWriter.NullableBoolValue.True);
			}
			return null;
		}

		private enum NullableBoolValue : byte
		{
			Null,
			True,
			False
		}
	}
}
