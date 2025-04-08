using System;

namespace Utf8Json.Formatters
{
	public static class EnumFormatterHelper
	{
		public static object GetSerializeDelegate(Type type, out bool isBoxed)
		{
			Type underlyingType = Enum.GetUnderlyingType(type);
			isBoxed = true;
			JsonSerializeAction<object> jsonSerializeAction;
			if (underlyingType == typeof(byte))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteByte((byte)value);
				};
			}
			else if (underlyingType == typeof(sbyte))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteSByte((sbyte)value);
				};
			}
			else if (underlyingType == typeof(short))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteInt16((short)value);
				};
			}
			else if (underlyingType == typeof(ushort))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteUInt16((ushort)value);
				};
			}
			else if (underlyingType == typeof(int))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteInt32((int)value);
				};
			}
			else if (underlyingType == typeof(uint))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteUInt32((uint)value);
				};
			}
			else if (underlyingType == typeof(long))
			{
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteInt64((long)value);
				};
			}
			else
			{
				if (!(underlyingType == typeof(ulong)))
				{
					throw new InvalidOperationException("Type is not Enum. Type:" + ((type != null) ? type.ToString() : null));
				}
				jsonSerializeAction = delegate(ref JsonWriter writer, object value, IJsonFormatterResolver _)
				{
					writer.WriteUInt64((ulong)value);
				};
			}
			return jsonSerializeAction;
		}

		public static object GetDeserializeDelegate(Type type, out bool isBoxed)
		{
			Type underlyingType = Enum.GetUnderlyingType(type);
			isBoxed = true;
			JsonDeserializeFunc<object> jsonDeserializeFunc;
			if (underlyingType == typeof(byte))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadByte();
				};
			}
			else if (underlyingType == typeof(sbyte))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadSByte();
				};
			}
			else if (underlyingType == typeof(short))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadInt16();
				};
			}
			else if (underlyingType == typeof(ushort))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadUInt16();
				};
			}
			else if (underlyingType == typeof(int))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadInt32();
				};
			}
			else if (underlyingType == typeof(uint))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadUInt32();
				};
			}
			else if (underlyingType == typeof(long))
			{
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadInt64();
				};
			}
			else
			{
				if (!(underlyingType == typeof(ulong)))
				{
					throw new InvalidOperationException("Type is not Enum. Type:" + ((type != null) ? type.ToString() : null));
				}
				jsonDeserializeFunc = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return reader.ReadUInt64();
				};
			}
			return jsonDeserializeFunc;
		}
	}
}
