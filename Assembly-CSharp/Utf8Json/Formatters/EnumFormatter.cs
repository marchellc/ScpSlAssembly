using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Utf8Json.Internal;

namespace Utf8Json.Formatters;

public class EnumFormatter<T> : IJsonFormatter<T>, IJsonFormatter, IObjectPropertyNameFormatter<T>
{
	private static readonly ByteArrayStringHashTable<T> nameValueMapping;

	private static readonly Dictionary<T, string> valueNameMapping;

	private static readonly JsonSerializeAction<T> defaultSerializeByUnderlyingValue;

	private static readonly JsonDeserializeFunc<T> defaultDeserializeByUnderlyingValue;

	private readonly bool serializeByName;

	private readonly JsonSerializeAction<T> serializeByUnderlyingValue;

	private readonly JsonDeserializeFunc<T> deserializeByUnderlyingValue;

	static EnumFormatter()
	{
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		Type type = typeof(T);
		foreach (FieldInfo item in from fi in type.GetFields()
			where fi.FieldType == type
			select fi)
		{
			object value = item.GetValue(null);
			string name = Enum.GetName(type, value);
			DataMemberAttribute dataMemberAttribute = item.GetCustomAttributes(typeof(DataMemberAttribute), inherit: true).OfType<DataMemberAttribute>().FirstOrDefault();
			EnumMemberAttribute enumMemberAttribute = item.GetCustomAttributes(typeof(EnumMemberAttribute), inherit: true).OfType<EnumMemberAttribute>().FirstOrDefault();
			list2.Add(value);
			list.Add((enumMemberAttribute != null && enumMemberAttribute.Value != null) ? enumMemberAttribute.Value : ((dataMemberAttribute != null && dataMemberAttribute.Name != null) ? dataMemberAttribute.Name : name));
		}
		EnumFormatter<T>.nameValueMapping = new ByteArrayStringHashTable<T>(list.Count);
		EnumFormatter<T>.valueNameMapping = new Dictionary<T, string>(list.Count);
		for (int num = 0; num < list.Count; num++)
		{
			EnumFormatter<T>.nameValueMapping.Add(JsonWriter.GetEncodedPropertyNameWithoutQuotation(list[num]), (T)list2[num]);
			EnumFormatter<T>.valueNameMapping[(T)list2[num]] = list[num];
		}
		bool isBoxed;
		object serializeDelegate = EnumFormatterHelper.GetSerializeDelegate(typeof(T), out isBoxed);
		if (isBoxed)
		{
			JsonSerializeAction<object> boxSerialize = (JsonSerializeAction<object>)serializeDelegate;
			EnumFormatter<T>.defaultSerializeByUnderlyingValue = delegate(ref JsonWriter writer, T val, IJsonFormatterResolver _)
			{
				boxSerialize(ref writer, val, _);
			};
		}
		else
		{
			EnumFormatter<T>.defaultSerializeByUnderlyingValue = (JsonSerializeAction<T>)serializeDelegate;
		}
		bool isBoxed2;
		object deserializeDelegate = EnumFormatterHelper.GetDeserializeDelegate(typeof(T), out isBoxed2);
		if (isBoxed2)
		{
			JsonDeserializeFunc<object> boxDeserialize = (JsonDeserializeFunc<object>)deserializeDelegate;
			EnumFormatter<T>.defaultDeserializeByUnderlyingValue = delegate(ref JsonReader reader, IJsonFormatterResolver _)
			{
				return (T)boxDeserialize(ref reader, _);
			};
		}
		else
		{
			EnumFormatter<T>.defaultDeserializeByUnderlyingValue = (JsonDeserializeFunc<T>)deserializeDelegate;
		}
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
			if (!EnumFormatter<T>.valueNameMapping.TryGetValue(value, out var value2))
			{
				value2 = value.ToString();
			}
			writer.WriteString(value2);
		}
		else
		{
			this.serializeByUnderlyingValue(ref writer, value, formatterResolver);
		}
	}

	public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		switch (reader.GetCurrentJsonToken())
		{
		case JsonToken.String:
		{
			ArraySegment<byte> key = reader.ReadStringSegmentUnsafe();
			if (!EnumFormatter<T>.nameValueMapping.TryGetValue(key, out var value))
			{
				string value2 = StringEncoding.UTF8.GetString(key.Array, key.Offset, key.Count);
				return (T)Enum.Parse(typeof(T), value2);
			}
			return value;
		}
		case JsonToken.Number:
			return this.deserializeByUnderlyingValue(ref reader, formatterResolver);
		default:
			throw new InvalidOperationException("Can't parse JSON to Enum format.");
		}
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
		T result = this.Deserialize(ref reader, formatterResolver);
		reader.SkipWhiteSpace();
		reader.AdvanceOffset(1);
		return result;
	}
}
