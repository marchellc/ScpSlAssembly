using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public class EnumFormatter<T> : IJsonFormatter<T>, IJsonFormatter, IObjectPropertyNameFormatter<T>
	{
		static EnumFormatter()
		{
			List<string> list = new List<string>();
			List<object> list2 = new List<object>();
			Type type = typeof(T);
			IEnumerable<FieldInfo> fields = type.GetFields();
			Func<FieldInfo, bool> <>9__0;
			Func<FieldInfo, bool> func;
			if ((func = <>9__0) == null)
			{
				func = (<>9__0 = (FieldInfo fi) => fi.FieldType == type);
			}
			foreach (FieldInfo fieldInfo in fields.Where(func))
			{
				object value2 = fieldInfo.GetValue(null);
				string name = Enum.GetName(type, value2);
				DataMemberAttribute dataMemberAttribute = fieldInfo.GetCustomAttributes(typeof(DataMemberAttribute), true).OfType<DataMemberAttribute>().FirstOrDefault<DataMemberAttribute>();
				EnumMemberAttribute enumMemberAttribute = fieldInfo.GetCustomAttributes(typeof(EnumMemberAttribute), true).OfType<EnumMemberAttribute>().FirstOrDefault<EnumMemberAttribute>();
				list2.Add(value2);
				list.Add((enumMemberAttribute != null && enumMemberAttribute.Value != null) ? enumMemberAttribute.Value : ((dataMemberAttribute != null && dataMemberAttribute.Name != null) ? dataMemberAttribute.Name : name));
			}
			EnumFormatter<T>.nameValueMapping = new ByteArrayStringHashTable<T>(list.Count);
			EnumFormatter<T>.valueNameMapping = new Dictionary<T, string>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				EnumFormatter<T>.nameValueMapping.Add(JsonWriter.GetEncodedPropertyNameWithoutQuotation(list[i]), (T)((object)list2[i]));
				EnumFormatter<T>.valueNameMapping[(T)((object)list2[i])] = list[i];
			}
			bool flag;
			object serializeDelegate = EnumFormatterHelper.GetSerializeDelegate(typeof(T), out flag);
			if (flag)
			{
				JsonSerializeAction<object> boxSerialize = (JsonSerializeAction<object>)serializeDelegate;
				EnumFormatter<T>.defaultSerializeByUnderlyingValue = delegate(ref JsonWriter writer, T value, IJsonFormatterResolver _)
				{
					boxSerialize(ref writer, value, _);
				};
			}
			else
			{
				EnumFormatter<T>.defaultSerializeByUnderlyingValue = (JsonSerializeAction<T>)serializeDelegate;
			}
			bool flag2;
			object deserializeDelegate = EnumFormatterHelper.GetDeserializeDelegate(typeof(T), out flag2);
			if (flag2)
			{
				JsonDeserializeFunc<object> boxDeserialize = (JsonDeserializeFunc<object>)deserializeDelegate;
				EnumFormatter<T>.defaultDeserializeByUnderlyingValue = delegate(ref JsonReader reader, IJsonFormatterResolver _)
				{
					return (T)((object)boxDeserialize(ref reader, _));
				};
				return;
			}
			EnumFormatter<T>.defaultDeserializeByUnderlyingValue = (JsonDeserializeFunc<T>)deserializeDelegate;
		}

		public EnumFormatter(bool serializeByName)
		{
			this.serializeByName = serializeByName;
			this.serializeByUnderlyingValue = EnumFormatter<T>.defaultSerializeByUnderlyingValue;
			this.deserializeByUnderlyingValue = EnumFormatter<T>.defaultDeserializeByUnderlyingValue;
		}

		public EnumFormatter(JsonSerializeAction<T> valueSerializeAction, JsonDeserializeFunc<T> valueDeserializeAction)
		{
			this.serializeByName = false;
			this.serializeByUnderlyingValue = valueSerializeAction;
			this.deserializeByUnderlyingValue = valueDeserializeAction;
		}

		public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
		{
			if (this.serializeByName)
			{
				string text;
				if (!EnumFormatter<T>.valueNameMapping.TryGetValue(value, out text))
				{
					text = value.ToString();
				}
				writer.WriteString(text);
				return;
			}
			this.serializeByUnderlyingValue(ref writer, value, formatterResolver);
		}

		public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			JsonToken currentJsonToken = reader.GetCurrentJsonToken();
			if (currentJsonToken == JsonToken.String)
			{
				ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
				T t;
				if (!EnumFormatter<T>.nameValueMapping.TryGetValue(arraySegment, out t))
				{
					string @string = StringEncoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
					t = (T)((object)Enum.Parse(typeof(T), @string));
				}
				return t;
			}
			if (currentJsonToken == JsonToken.Number)
			{
				return this.deserializeByUnderlyingValue(ref reader, formatterResolver);
			}
			throw new InvalidOperationException("Can't parse JSON to Enum format.");
		}

		public void SerializeToPropertyName(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
		{
			if (this.serializeByName)
			{
				this.Serialize(ref writer, value, formatterResolver);
				return;
			}
			writer.WriteQuotation();
			this.Serialize(ref writer, value, formatterResolver);
			writer.WriteQuotation();
		}

		public T DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (this.serializeByName)
			{
				return this.Deserialize(ref reader, formatterResolver);
			}
			if (reader.GetCurrentJsonToken() != JsonToken.String)
			{
				throw new InvalidOperationException("Can't parse JSON to Enum format.");
			}
			reader.AdvanceOffset(1);
			T t = this.Deserialize(ref reader, formatterResolver);
			reader.SkipWhiteSpace();
			reader.AdvanceOffset(1);
			return t;
		}

		private static readonly ByteArrayStringHashTable<T> nameValueMapping;

		private static readonly Dictionary<T, string> valueNameMapping;

		private static readonly JsonSerializeAction<T> defaultSerializeByUnderlyingValue;

		private static readonly JsonDeserializeFunc<T> defaultDeserializeByUnderlyingValue;

		private readonly bool serializeByName;

		private readonly JsonSerializeAction<T> serializeByUnderlyingValue;

		private readonly JsonDeserializeFunc<T> deserializeByUnderlyingValue;
	}
}
