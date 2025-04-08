using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;
using Utf8Json.Resolvers;

namespace Utf8Json
{
	public static class JsonSerializer
	{
		public static IJsonFormatterResolver DefaultResolver
		{
			get
			{
				if (JsonSerializer.defaultResolver == null)
				{
					JsonSerializer.defaultResolver = StandardResolver.Default;
				}
				return JsonSerializer.defaultResolver;
			}
		}

		public static bool IsInitialized
		{
			get
			{
				return JsonSerializer.defaultResolver != null;
			}
		}

		public static void SetDefaultResolver(IJsonFormatterResolver resolver)
		{
			JsonSerializer.defaultResolver = resolver;
		}

		public static byte[] Serialize<T>(T obj)
		{
			return JsonSerializer.Serialize<T>(obj, JsonSerializer.defaultResolver);
		}

		public static byte[] Serialize<T>(T value, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			resolver.GetFormatterWithVerify<T>().Serialize(ref jsonWriter, value, resolver);
			return jsonWriter.ToUtf8ByteArray();
		}

		public static void Serialize<T>(ref JsonWriter writer, T value)
		{
			JsonSerializer.Serialize<T>(ref writer, value, JsonSerializer.defaultResolver);
		}

		public static void Serialize<T>(ref JsonWriter writer, T value, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, resolver);
		}

		public static void Serialize<T>(Stream stream, T value)
		{
			JsonSerializer.Serialize<T>(stream, value, JsonSerializer.defaultResolver);
		}

		public static void Serialize<T>(Stream stream, T value, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			ArraySegment<byte> arraySegment = JsonSerializer.SerializeUnsafe<T>(value, resolver);
			stream.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		public static ArraySegment<byte> SerializeUnsafe<T>(T obj)
		{
			return JsonSerializer.SerializeUnsafe<T>(obj, JsonSerializer.defaultResolver);
		}

		public static ArraySegment<byte> SerializeUnsafe<T>(T value, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			resolver.GetFormatterWithVerify<T>().Serialize(ref jsonWriter, value, resolver);
			return jsonWriter.GetBuffer();
		}

		public static string ToJsonString<T>(T value)
		{
			return JsonSerializer.ToJsonString<T>(value, JsonSerializer.defaultResolver);
		}

		public static string ToJsonString<T>(T value, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			resolver.GetFormatterWithVerify<T>().Serialize(ref jsonWriter, value, resolver);
			return jsonWriter.ToString();
		}

		public static T Deserialize<T>(string json)
		{
			return JsonSerializer.Deserialize<T>(json, JsonSerializer.defaultResolver);
		}

		public static T Deserialize<T>(string json, IJsonFormatterResolver resolver)
		{
			return JsonSerializer.Deserialize<T>(StringEncoding.UTF8.GetBytes(json), resolver);
		}

		public static T Deserialize<T>(byte[] bytes)
		{
			return JsonSerializer.Deserialize<T>(bytes, JsonSerializer.defaultResolver);
		}

		public static T Deserialize<T>(byte[] bytes, IJsonFormatterResolver resolver)
		{
			return JsonSerializer.Deserialize<T>(bytes, 0, resolver);
		}

		public static T Deserialize<T>(byte[] bytes, int offset)
		{
			return JsonSerializer.Deserialize<T>(bytes, offset, JsonSerializer.defaultResolver);
		}

		public static T Deserialize<T>(byte[] bytes, int offset, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			JsonReader jsonReader = new JsonReader(bytes, offset);
			return resolver.GetFormatterWithVerify<T>().Deserialize(ref jsonReader, resolver);
		}

		public static T Deserialize<T>(ref JsonReader reader)
		{
			return JsonSerializer.Deserialize<T>(ref reader, JsonSerializer.defaultResolver);
		}

		public static T Deserialize<T>(ref JsonReader reader, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			return resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, resolver);
		}

		public static T Deserialize<T>(Stream stream)
		{
			return JsonSerializer.Deserialize<T>(stream, JsonSerializer.defaultResolver);
		}

		public static T Deserialize<T>(Stream stream, IJsonFormatterResolver resolver)
		{
			if (resolver == null)
			{
				resolver = JsonSerializer.DefaultResolver;
			}
			byte[] array = JsonSerializer.MemoryPool.GetBuffer();
			int num = JsonSerializer.FillFromStream(stream, ref array);
			if (new JsonReader(array).GetCurrentJsonToken() == JsonToken.Number)
			{
				array = BinaryUtil.FastCloneWithResize(array, num);
			}
			return JsonSerializer.Deserialize<T>(array, resolver);
		}

		public static string PrettyPrint(byte[] json)
		{
			return JsonSerializer.PrettyPrint(json, 0);
		}

		public static string PrettyPrint(byte[] json, int offset)
		{
			JsonReader jsonReader = new JsonReader(json, offset);
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			JsonSerializer.WritePrittyPrint(ref jsonReader, ref jsonWriter, 0);
			return jsonWriter.ToString();
		}

		public static string PrettyPrint(string json)
		{
			JsonReader jsonReader = new JsonReader(Encoding.UTF8.GetBytes(json));
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			JsonSerializer.WritePrittyPrint(ref jsonReader, ref jsonWriter, 0);
			return jsonWriter.ToString();
		}

		public static byte[] PrettyPrintByteArray(byte[] json)
		{
			return JsonSerializer.PrettyPrintByteArray(json, 0);
		}

		public static byte[] PrettyPrintByteArray(byte[] json, int offset)
		{
			JsonReader jsonReader = new JsonReader(json, offset);
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			JsonSerializer.WritePrittyPrint(ref jsonReader, ref jsonWriter, 0);
			return jsonWriter.ToUtf8ByteArray();
		}

		public static byte[] PrettyPrintByteArray(string json)
		{
			JsonReader jsonReader = new JsonReader(Encoding.UTF8.GetBytes(json));
			JsonWriter jsonWriter = new JsonWriter(JsonSerializer.MemoryPool.GetBuffer());
			JsonSerializer.WritePrittyPrint(ref jsonReader, ref jsonWriter, 0);
			return jsonWriter.ToUtf8ByteArray();
		}

		private static void WritePrittyPrint(ref JsonReader reader, ref JsonWriter writer, int depth)
		{
			switch (reader.GetCurrentJsonToken())
			{
			case JsonToken.BeginObject:
			{
				writer.WriteBeginObject();
				writer.WriteRaw(JsonSerializer.newLine);
				int num = 0;
				while (reader.ReadIsInObject(ref num))
				{
					if (num != 1)
					{
						writer.WriteRaw(44);
						writer.WriteRaw(JsonSerializer.newLine);
					}
					writer.WriteRaw(JsonSerializer.indent[depth + 1]);
					writer.WritePropertyName(reader.ReadPropertyName());
					writer.WriteRaw(32);
					JsonSerializer.WritePrittyPrint(ref reader, ref writer, depth + 1);
				}
				writer.WriteRaw(JsonSerializer.newLine);
				writer.WriteRaw(JsonSerializer.indent[depth]);
				writer.WriteEndObject();
				return;
			}
			case JsonToken.EndObject:
			case JsonToken.EndArray:
				break;
			case JsonToken.BeginArray:
			{
				writer.WriteBeginArray();
				writer.WriteRaw(JsonSerializer.newLine);
				int num2 = 0;
				while (reader.ReadIsInArray(ref num2))
				{
					if (num2 != 1)
					{
						writer.WriteRaw(44);
						writer.WriteRaw(JsonSerializer.newLine);
					}
					writer.WriteRaw(JsonSerializer.indent[depth + 1]);
					JsonSerializer.WritePrittyPrint(ref reader, ref writer, depth + 1);
				}
				writer.WriteRaw(JsonSerializer.newLine);
				writer.WriteRaw(JsonSerializer.indent[depth]);
				writer.WriteEndArray();
				return;
			}
			case JsonToken.Number:
			{
				double num3 = reader.ReadDouble();
				writer.WriteDouble(num3);
				return;
			}
			case JsonToken.String:
			{
				string text = reader.ReadString();
				writer.WriteString(text);
				return;
			}
			case JsonToken.True:
			case JsonToken.False:
			{
				bool flag = reader.ReadBoolean();
				writer.WriteBoolean(flag);
				return;
			}
			case JsonToken.Null:
				reader.ReadIsNull();
				writer.WriteNull();
				break;
			default:
				return;
			}
		}

		private static int FillFromStream(Stream input, ref byte[] buffer)
		{
			int num = 0;
			int num2;
			while ((num2 = input.Read(buffer, num, buffer.Length - num)) > 0)
			{
				num += num2;
				if (num == buffer.Length)
				{
					BinaryUtil.FastResize(ref buffer, num * 2);
				}
			}
			return num;
		}

		private static IJsonFormatterResolver defaultResolver;

		private static readonly byte[][] indent = (from x in Enumerable.Range(0, 100)
			select Encoding.UTF8.GetBytes(new string(' ', x * 2))).ToArray<byte[]>();

		private static readonly byte[] newLine = Encoding.UTF8.GetBytes(Environment.NewLine);

		private static class MemoryPool
		{
			public static byte[] GetBuffer()
			{
				if (JsonSerializer.MemoryPool.buffer == null)
				{
					JsonSerializer.MemoryPool.buffer = new byte[65536];
				}
				return JsonSerializer.MemoryPool.buffer;
			}

			[ThreadStatic]
			private static byte[] buffer;
		}

		public static class NonGeneric
		{
			private static JsonSerializer.NonGeneric.CompiledMethods GetOrAdd(Type type)
			{
				return JsonSerializer.NonGeneric.serializes.GetOrAdd(type, JsonSerializer.NonGeneric.CreateCompiledMethods);
			}

			public static byte[] Serialize(object value)
			{
				if (value == null)
				{
					return JsonSerializer.Serialize<object>(value);
				}
				return JsonSerializer.NonGeneric.Serialize(value.GetType(), value, JsonSerializer.defaultResolver);
			}

			public static byte[] Serialize(Type type, object value)
			{
				return JsonSerializer.NonGeneric.Serialize(type, value, JsonSerializer.defaultResolver);
			}

			public static byte[] Serialize(object value, IJsonFormatterResolver resolver)
			{
				if (value == null)
				{
					return JsonSerializer.Serialize<object>(value, resolver);
				}
				return JsonSerializer.NonGeneric.Serialize(value.GetType(), value, resolver);
			}

			public static byte[] Serialize(Type type, object value, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).serialize1(value, resolver);
			}

			public static void Serialize(Stream stream, object value)
			{
				if (value == null)
				{
					JsonSerializer.Serialize<object>(stream, value);
					return;
				}
				JsonSerializer.NonGeneric.Serialize(value.GetType(), stream, value, JsonSerializer.defaultResolver);
			}

			public static void Serialize(Type type, Stream stream, object value)
			{
				JsonSerializer.NonGeneric.Serialize(type, stream, value, JsonSerializer.defaultResolver);
			}

			public static void Serialize(Stream stream, object value, IJsonFormatterResolver resolver)
			{
				if (value == null)
				{
					JsonSerializer.Serialize<object>(stream, value, resolver);
					return;
				}
				JsonSerializer.NonGeneric.Serialize(value.GetType(), stream, value, resolver);
			}

			public static void Serialize(Type type, Stream stream, object value, IJsonFormatterResolver resolver)
			{
				JsonSerializer.NonGeneric.GetOrAdd(type).serialize2(stream, value, resolver);
			}

			public static void Serialize(ref JsonWriter writer, object value, IJsonFormatterResolver resolver)
			{
				if (value == null)
				{
					writer.WriteNull();
					return;
				}
				JsonSerializer.NonGeneric.Serialize(value.GetType(), ref writer, value, resolver);
			}

			public static void Serialize(Type type, ref JsonWriter writer, object value)
			{
				JsonSerializer.NonGeneric.Serialize(type, ref writer, value, JsonSerializer.defaultResolver);
			}

			public static void Serialize(Type type, ref JsonWriter writer, object value, IJsonFormatterResolver resolver)
			{
				JsonSerializer.NonGeneric.GetOrAdd(type).serialize3(ref writer, value, resolver);
			}

			public static ArraySegment<byte> SerializeUnsafe(object value)
			{
				if (value == null)
				{
					return JsonSerializer.SerializeUnsafe<object>(value);
				}
				return JsonSerializer.NonGeneric.SerializeUnsafe(value.GetType(), value);
			}

			public static ArraySegment<byte> SerializeUnsafe(Type type, object value)
			{
				return JsonSerializer.NonGeneric.SerializeUnsafe(type, value, JsonSerializer.defaultResolver);
			}

			public static ArraySegment<byte> SerializeUnsafe(object value, IJsonFormatterResolver resolver)
			{
				if (value == null)
				{
					return JsonSerializer.SerializeUnsafe<object>(value);
				}
				return JsonSerializer.NonGeneric.SerializeUnsafe(value.GetType(), value, resolver);
			}

			public static ArraySegment<byte> SerializeUnsafe(Type type, object value, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).serializeUnsafe(value, resolver);
			}

			public static string ToJsonString(object value)
			{
				if (value == null)
				{
					return "null";
				}
				return JsonSerializer.NonGeneric.ToJsonString(value.GetType(), value);
			}

			public static string ToJsonString(Type type, object value)
			{
				return JsonSerializer.NonGeneric.ToJsonString(type, value, JsonSerializer.defaultResolver);
			}

			public static string ToJsonString(object value, IJsonFormatterResolver resolver)
			{
				if (value == null)
				{
					return "null";
				}
				return JsonSerializer.NonGeneric.ToJsonString(value.GetType(), value, resolver);
			}

			public static string ToJsonString(Type type, object value, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).toJsonString(value, resolver);
			}

			public static object Deserialize(Type type, string json)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, json, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, string json, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).deserialize1(json, resolver);
			}

			public static object Deserialize(Type type, byte[] bytes)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, bytes, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, byte[] bytes, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, bytes, 0, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, byte[] bytes, int offset)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, bytes, offset, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, byte[] bytes, int offset, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).deserialize2(bytes, offset, resolver);
			}

			public static object Deserialize(Type type, Stream stream)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, stream, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, Stream stream, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).deserialize3(stream, resolver);
			}

			public static object Deserialize(Type type, ref JsonReader reader)
			{
				return JsonSerializer.NonGeneric.Deserialize(type, ref reader, JsonSerializer.defaultResolver);
			}

			public static object Deserialize(Type type, ref JsonReader reader, IJsonFormatterResolver resolver)
			{
				return JsonSerializer.NonGeneric.GetOrAdd(type).deserialize4(ref reader, resolver);
			}

			private static readonly Func<Type, JsonSerializer.NonGeneric.CompiledMethods> CreateCompiledMethods = (Type t) => new JsonSerializer.NonGeneric.CompiledMethods(t);

			private static readonly ThreadsafeTypeKeyHashTable<JsonSerializer.NonGeneric.CompiledMethods> serializes = new ThreadsafeTypeKeyHashTable<JsonSerializer.NonGeneric.CompiledMethods>(64, 0.75f);

			private delegate void SerializeJsonWriter(ref JsonWriter writer, object value, IJsonFormatterResolver resolver);

			private delegate object DeserializeJsonReader(ref JsonReader reader, IJsonFormatterResolver resolver);

			private class CompiledMethods
			{
				public CompiledMethods(Type type)
				{
					DynamicMethod dynamicMethod = new DynamicMethod("serialize1", typeof(byte[]), new Type[]
					{
						typeof(object),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
					ilgenerator.EmitLdarg(0);
					ilgenerator.EmitUnboxOrCast(type);
					ilgenerator.EmitLdarg(1);
					ilgenerator.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Serialize", new Type[]
					{
						null,
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator.Emit(OpCodes.Ret);
					this.serialize1 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<object, IJsonFormatterResolver, byte[]>>(dynamicMethod);
					DynamicMethod dynamicMethod2 = new DynamicMethod("serialize2", null, new Type[]
					{
						typeof(Stream),
						typeof(object),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator2 = dynamicMethod2.GetILGenerator();
					ilgenerator2.EmitLdarg(0);
					ilgenerator2.EmitLdarg(1);
					ilgenerator2.EmitUnboxOrCast(type);
					ilgenerator2.EmitLdarg(2);
					ilgenerator2.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Serialize", new Type[]
					{
						typeof(Stream),
						null,
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator2.Emit(OpCodes.Ret);
					this.serialize2 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Action<Stream, object, IJsonFormatterResolver>>(dynamicMethod2);
					DynamicMethod dynamicMethod3 = new DynamicMethod("serialize3", null, new Type[]
					{
						typeof(JsonWriter).MakeByRefType(),
						typeof(object),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator3 = dynamicMethod3.GetILGenerator();
					ilgenerator3.EmitLdarg(0);
					ilgenerator3.EmitLdarg(1);
					ilgenerator3.EmitUnboxOrCast(type);
					ilgenerator3.EmitLdarg(2);
					ilgenerator3.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Serialize", new Type[]
					{
						typeof(JsonWriter).MakeByRefType(),
						null,
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator3.Emit(OpCodes.Ret);
					this.serialize3 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<JsonSerializer.NonGeneric.SerializeJsonWriter>(dynamicMethod3);
					DynamicMethod dynamicMethod4 = new DynamicMethod("serializeUnsafe", typeof(ArraySegment<byte>), new Type[]
					{
						typeof(object),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator4 = dynamicMethod4.GetILGenerator();
					ilgenerator4.EmitLdarg(0);
					ilgenerator4.EmitUnboxOrCast(type);
					ilgenerator4.EmitLdarg(1);
					ilgenerator4.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "SerializeUnsafe", new Type[]
					{
						null,
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator4.Emit(OpCodes.Ret);
					this.serializeUnsafe = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<object, IJsonFormatterResolver, ArraySegment<byte>>>(dynamicMethod4);
					DynamicMethod dynamicMethod5 = new DynamicMethod("toJsonString", typeof(string), new Type[]
					{
						typeof(object),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator5 = dynamicMethod5.GetILGenerator();
					ilgenerator5.EmitLdarg(0);
					ilgenerator5.EmitUnboxOrCast(type);
					ilgenerator5.EmitLdarg(1);
					ilgenerator5.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "ToJsonString", new Type[]
					{
						null,
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator5.Emit(OpCodes.Ret);
					this.toJsonString = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<object, IJsonFormatterResolver, string>>(dynamicMethod5);
					DynamicMethod dynamicMethod6 = new DynamicMethod("Deserialize", typeof(object), new Type[]
					{
						typeof(string),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator6 = dynamicMethod6.GetILGenerator();
					ilgenerator6.EmitLdarg(0);
					ilgenerator6.EmitLdarg(1);
					ilgenerator6.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Deserialize", new Type[]
					{
						typeof(string),
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator6.EmitBoxOrDoNothing(type);
					ilgenerator6.Emit(OpCodes.Ret);
					this.deserialize1 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<string, IJsonFormatterResolver, object>>(dynamicMethod6);
					DynamicMethod dynamicMethod7 = new DynamicMethod("Deserialize", typeof(object), new Type[]
					{
						typeof(byte[]),
						typeof(int),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator7 = dynamicMethod7.GetILGenerator();
					ilgenerator7.EmitLdarg(0);
					ilgenerator7.EmitLdarg(1);
					ilgenerator7.EmitLdarg(2);
					ilgenerator7.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Deserialize", new Type[]
					{
						typeof(byte[]),
						typeof(int),
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator7.EmitBoxOrDoNothing(type);
					ilgenerator7.Emit(OpCodes.Ret);
					this.deserialize2 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<byte[], int, IJsonFormatterResolver, object>>(dynamicMethod7);
					DynamicMethod dynamicMethod8 = new DynamicMethod("Deserialize", typeof(object), new Type[]
					{
						typeof(Stream),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator8 = dynamicMethod8.GetILGenerator();
					ilgenerator8.EmitLdarg(0);
					ilgenerator8.EmitLdarg(1);
					ilgenerator8.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Deserialize", new Type[]
					{
						typeof(Stream),
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator8.EmitBoxOrDoNothing(type);
					ilgenerator8.Emit(OpCodes.Ret);
					this.deserialize3 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<Func<Stream, IJsonFormatterResolver, object>>(dynamicMethod8);
					DynamicMethod dynamicMethod9 = new DynamicMethod("Deserialize", typeof(object), new Type[]
					{
						typeof(JsonReader).MakeByRefType(),
						typeof(IJsonFormatterResolver)
					}, type.Module, true);
					ILGenerator ilgenerator9 = dynamicMethod9.GetILGenerator();
					ilgenerator9.EmitLdarg(0);
					ilgenerator9.EmitLdarg(1);
					ilgenerator9.EmitCall(JsonSerializer.NonGeneric.CompiledMethods.GetMethod(type, "Deserialize", new Type[]
					{
						typeof(JsonReader).MakeByRefType(),
						typeof(IJsonFormatterResolver)
					}));
					ilgenerator9.EmitBoxOrDoNothing(type);
					ilgenerator9.Emit(OpCodes.Ret);
					this.deserialize4 = JsonSerializer.NonGeneric.CompiledMethods.CreateDelegate<JsonSerializer.NonGeneric.DeserializeJsonReader>(dynamicMethod9);
				}

				private static T CreateDelegate<T>(DynamicMethod dm)
				{
					return (T)((object)dm.CreateDelegate(typeof(T)));
				}

				private static MethodInfo GetMethod(Type type, string name, Type[] arguments)
				{
					return (from x in typeof(JsonSerializer).GetMethods(BindingFlags.Static | BindingFlags.Public)
						where x.Name == name
						select x).Single(delegate(MethodInfo x)
					{
						ParameterInfo[] parameters = x.GetParameters();
						if (parameters.Length != arguments.Length)
						{
							return false;
						}
						for (int i = 0; i < parameters.Length; i++)
						{
							if ((!(arguments[i] == null) || !parameters[i].ParameterType.IsGenericParameter) && parameters[i].ParameterType != arguments[i])
							{
								return false;
							}
						}
						return true;
					}).MakeGenericMethod(new Type[] { type });
				}

				public readonly Func<object, IJsonFormatterResolver, byte[]> serialize1;

				public readonly Action<Stream, object, IJsonFormatterResolver> serialize2;

				public readonly JsonSerializer.NonGeneric.SerializeJsonWriter serialize3;

				public readonly Func<object, IJsonFormatterResolver, ArraySegment<byte>> serializeUnsafe;

				public readonly Func<object, IJsonFormatterResolver, string> toJsonString;

				public readonly Func<string, IJsonFormatterResolver, object> deserialize1;

				public readonly Func<byte[], int, IJsonFormatterResolver, object> deserialize2;

				public readonly Func<Stream, IJsonFormatterResolver, object> deserialize3;

				public readonly JsonSerializer.NonGeneric.DeserializeJsonReader deserialize4;
			}
		}
	}
}
