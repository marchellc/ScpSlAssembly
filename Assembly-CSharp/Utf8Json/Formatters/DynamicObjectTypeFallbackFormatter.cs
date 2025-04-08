using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;
using Utf8Json.Resolvers.Internal;

namespace Utf8Json.Formatters
{
	public sealed class DynamicObjectTypeFallbackFormatter : IJsonFormatter<object>, IJsonFormatter
	{
		public DynamicObjectTypeFallbackFormatter(params IJsonFormatterResolver[] innerResolvers)
		{
			this.innerResolvers = innerResolvers;
		}

		public void Serialize(ref JsonWriter writer, object value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			Type type = value.GetType();
			if (type == typeof(object))
			{
				writer.WriteBeginObject();
				writer.WriteEndObject();
				return;
			}
			KeyValuePair<object, DynamicObjectTypeFallbackFormatter.SerializeMethod> keyValuePair;
			if (!this.serializers.TryGetValue(type, out keyValuePair))
			{
				ThreadsafeTypeKeyHashTable<KeyValuePair<object, DynamicObjectTypeFallbackFormatter.SerializeMethod>> threadsafeTypeKeyHashTable = this.serializers;
				lock (threadsafeTypeKeyHashTable)
				{
					if (!this.serializers.TryGetValue(type, out keyValuePair))
					{
						object obj = null;
						IJsonFormatterResolver[] array = this.innerResolvers;
						for (int i = 0; i < array.Length; i++)
						{
							obj = array[i].GetFormatterDynamic(type);
							if (obj != null)
							{
								break;
							}
						}
						if (obj == null)
						{
							throw new FormatterNotRegisteredException(type.FullName + " is not registered in this resolver. resolvers:" + string.Join(", ", this.innerResolvers.Select((IJsonFormatterResolver x) => x.GetType().Name).ToArray<string>()));
						}
						Type type2 = type;
						DynamicMethod dynamicMethod = new DynamicMethod("Serialize", null, new Type[]
						{
							typeof(object),
							typeof(JsonWriter).MakeByRefType(),
							typeof(object),
							typeof(IJsonFormatterResolver)
						}, type.Module, true);
						ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
						ilgenerator.EmitLdarg(0);
						ilgenerator.Emit(OpCodes.Castclass, typeof(IJsonFormatter<>).MakeGenericType(new Type[] { type2 }));
						ilgenerator.EmitLdarg(1);
						ilgenerator.EmitLdarg(2);
						ilgenerator.EmitUnboxOrCast(type2);
						ilgenerator.EmitLdarg(3);
						ilgenerator.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Serialize(type2));
						ilgenerator.Emit(OpCodes.Ret);
						keyValuePair = new KeyValuePair<object, DynamicObjectTypeFallbackFormatter.SerializeMethod>(obj, (DynamicObjectTypeFallbackFormatter.SerializeMethod)dynamicMethod.CreateDelegate(typeof(DynamicObjectTypeFallbackFormatter.SerializeMethod)));
						this.serializers.TryAdd(type2, keyValuePair);
					}
				}
			}
			keyValuePair.Value(keyValuePair.Key, ref writer, value, formatterResolver);
		}

		public object Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			return PrimitiveObjectFormatter.Default.Deserialize(ref reader, formatterResolver);
		}

		private readonly ThreadsafeTypeKeyHashTable<KeyValuePair<object, DynamicObjectTypeFallbackFormatter.SerializeMethod>> serializers = new ThreadsafeTypeKeyHashTable<KeyValuePair<object, DynamicObjectTypeFallbackFormatter.SerializeMethod>>(4, 0.75f);

		private readonly IJsonFormatterResolver[] innerResolvers;

		private delegate void SerializeMethod(object dynamicFormatter, ref JsonWriter writer, object value, IJsonFormatterResolver formatterResolver);
	}
}
