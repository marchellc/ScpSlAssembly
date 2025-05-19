namespace Utf8Json;

public interface IJsonFormatter
{
}
public interface IJsonFormatter<T> : IJsonFormatter
{
	void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver);

	T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
}
