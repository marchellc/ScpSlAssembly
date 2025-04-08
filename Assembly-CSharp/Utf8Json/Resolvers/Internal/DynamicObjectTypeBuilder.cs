using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Utf8Json.Formatters;
using Utf8Json.Internal;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers.Internal
{
	internal static class DynamicObjectTypeBuilder
	{
		public static object BuildFormatterToAssembly<T>(DynamicAssembly assembly, IJsonFormatterResolver selfResolver, Func<string, string> nameMutator, bool excludeNull)
		{
			TypeInfo typeInfo = typeof(T).GetTypeInfo();
			if (typeInfo.IsNullable())
			{
				typeInfo = typeInfo.GenericTypeArguments[0].GetTypeInfo();
				object formatterDynamic = selfResolver.GetFormatterDynamic(typeInfo.AsType());
				if (formatterDynamic == null)
				{
					return null;
				}
				return (IJsonFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(new Type[] { typeInfo.AsType() }), new object[] { formatterDynamic });
			}
			else
			{
				if (typeof(Exception).GetTypeInfo().IsAssignableFrom(typeInfo))
				{
					return DynamicObjectTypeBuilder.BuildAnonymousFormatter(typeof(T), nameMutator, excludeNull, false, true);
				}
				Type type;
				if (typeInfo.IsAnonymous() || DynamicObjectTypeBuilder.TryGetInterfaceEnumerableElementType(typeof(T), out type))
				{
					return DynamicObjectTypeBuilder.BuildAnonymousFormatter(typeof(T), nameMutator, excludeNull, false, false);
				}
				TypeInfo typeInfo2 = DynamicObjectTypeBuilder.BuildType(assembly, typeof(T), nameMutator, excludeNull);
				if (typeInfo2 == null)
				{
					return null;
				}
				return (IJsonFormatter<T>)Activator.CreateInstance(typeInfo2.AsType());
			}
		}

		public static object BuildFormatterToDynamicMethod<T>(IJsonFormatterResolver selfResolver, Func<string, string> nameMutator, bool excludeNull, bool allowPrivate)
		{
			TypeInfo typeInfo = typeof(T).GetTypeInfo();
			if (typeInfo.IsNullable())
			{
				typeInfo = typeInfo.GenericTypeArguments[0].GetTypeInfo();
				object formatterDynamic = selfResolver.GetFormatterDynamic(typeInfo.AsType());
				if (formatterDynamic == null)
				{
					return null;
				}
				return (IJsonFormatter<T>)Activator.CreateInstance(typeof(StaticNullableFormatter<>).MakeGenericType(new Type[] { typeInfo.AsType() }), new object[] { formatterDynamic });
			}
			else
			{
				if (typeof(Exception).GetTypeInfo().IsAssignableFrom(typeInfo))
				{
					return DynamicObjectTypeBuilder.BuildAnonymousFormatter(typeof(T), nameMutator, excludeNull, false, true);
				}
				return DynamicObjectTypeBuilder.BuildAnonymousFormatter(typeof(T), nameMutator, excludeNull, allowPrivate, false);
			}
		}

		private static TypeInfo BuildType(DynamicAssembly assembly, Type type, Func<string, string> nameMutator, bool excludeNull)
		{
			DynamicObjectTypeBuilder.<>c__DisplayClass6_0 CS$<>8__locals1 = new DynamicObjectTypeBuilder.<>c__DisplayClass6_0();
			if (DynamicObjectTypeBuilder.ignoreTypes.Contains(type))
			{
				return null;
			}
			MetaType metaType = new MetaType(type, nameMutator, false);
			bool flag = metaType.Members.Any((MetaMember x) => x.ShouldSerializeMethodInfo != null);
			Type type2 = typeof(IJsonFormatter<>).MakeGenericType(new Type[] { type });
			TypeBuilder typeBuilder = assembly.DefineType("Utf8Json.Formatters." + DynamicObjectTypeBuilder.SubtractFullNameRegex.Replace(type.FullName, "").Replace(".", "_") + "Formatter" + Interlocked.Increment(ref DynamicObjectTypeBuilder.nameSequence).ToString(), TypeAttributes.Public | TypeAttributes.Sealed, null, new Type[] { type2 });
			ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
			CS$<>8__locals1.stringByteKeysField = typeBuilder.DefineField("stringByteKeys", typeof(byte[][]), FieldAttributes.Private | FieldAttributes.InitOnly);
			ILGenerator ilgenerator = constructorBuilder.GetILGenerator();
			CS$<>8__locals1.customFormatterLookup = DynamicObjectTypeBuilder.BuildConstructor(typeBuilder, metaType, constructorBuilder, CS$<>8__locals1.stringByteKeysField, ilgenerator, excludeNull, flag);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("Serialize", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Final | MethodAttributes.Virtual, null, new Type[]
			{
				typeof(JsonWriter).MakeByRefType(),
				type,
				typeof(IJsonFormatterResolver)
			});
			ILGenerator il2 = methodBuilder.GetILGenerator();
			DynamicObjectTypeBuilder.BuildSerialize(type, metaType, il2, delegate
			{
				il2.EmitLoadThis();
				il2.EmitLdfld(CS$<>8__locals1.stringByteKeysField);
			}, delegate(int index, MetaMember member)
			{
				FieldInfo fieldInfo;
				if (!CS$<>8__locals1.customFormatterLookup.TryGetValue(member, out fieldInfo))
				{
					return false;
				}
				il2.EmitLoadThis();
				il2.EmitLdfld(fieldInfo);
				return true;
			}, excludeNull, flag, 1);
			MethodBuilder methodBuilder2 = typeBuilder.DefineMethod("Deserialize", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Final | MethodAttributes.Virtual, type, new Type[]
			{
				typeof(JsonReader).MakeByRefType(),
				typeof(IJsonFormatterResolver)
			});
			ILGenerator il = methodBuilder2.GetILGenerator();
			DynamicObjectTypeBuilder.BuildDeserialize(type, metaType, il, delegate(int index, MetaMember member)
			{
				FieldInfo fieldInfo2;
				if (!CS$<>8__locals1.customFormatterLookup.TryGetValue(member, out fieldInfo2))
				{
					return false;
				}
				il.EmitLoadThis();
				il.EmitLdfld(fieldInfo2);
				return true;
			}, false, 1);
			return typeBuilder.CreateTypeInfo();
		}

		public static object BuildAnonymousFormatter(Type type, Func<string, string> nameMutator, bool excludeNull, bool allowPrivate, bool isException)
		{
			DynamicObjectTypeBuilder.<>c__DisplayClass7_0 CS$<>8__locals1 = new DynamicObjectTypeBuilder.<>c__DisplayClass7_0();
			CS$<>8__locals1.nameMutator = nameMutator;
			if (DynamicObjectTypeBuilder.ignoreTypes.Contains(type))
			{
				return false;
			}
			MetaType metaType;
			if (isException)
			{
				HashSet<string> ignoreSet = new HashSet<string>(new string[] { "HelpLink", "TargetSite", "HResult", "Data", "ClassName", "InnerException" }.Select((string x) => CS$<>8__locals1.nameMutator(x)));
				metaType = new MetaType(type, CS$<>8__locals1.nameMutator, false);
				metaType.BestmatchConstructor = null;
				metaType.ConstructorParameters = new MetaMember[0];
				metaType.Members = new StringConstantValueMetaMember[]
				{
					new StringConstantValueMetaMember(CS$<>8__locals1.nameMutator("ClassName"), type.FullName)
				}.Concat(metaType.Members.Where((MetaMember x) => !ignoreSet.Contains(x.Name))).Concat(new InnerExceptionMetaMember[]
				{
					new InnerExceptionMetaMember(CS$<>8__locals1.nameMutator("InnerException"))
				}).ToArray<MetaMember>();
			}
			else
			{
				metaType = new MetaType(type, CS$<>8__locals1.nameMutator, allowPrivate);
			}
			bool flag = metaType.Members.Any((MetaMember x) => x.ShouldSerializeMethodInfo != null);
			List<byte[]> list = new List<byte[]>();
			int num = 0;
			foreach (MetaMember metaMember in metaType.Members.Where((MetaMember x) => x.IsReadable))
			{
				if (excludeNull || flag)
				{
					list.Add(JsonWriter.GetEncodedPropertyName(metaMember.Name));
				}
				else if (num == 0)
				{
					list.Add(JsonWriter.GetEncodedPropertyNameWithBeginObject(metaMember.Name));
				}
				else
				{
					list.Add(JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator(metaMember.Name));
				}
				num++;
			}
			CS$<>8__locals1.serializeCustomFormatters = new List<object>();
			CS$<>8__locals1.deserializeCustomFormatters = new List<object>();
			foreach (MetaMember metaMember2 in metaType.Members.Where((MetaMember x) => x.IsReadable))
			{
				JsonFormatterAttribute customAttribute = metaMember2.GetCustomAttribute<JsonFormatterAttribute>(true);
				if (customAttribute != null)
				{
					object obj = Activator.CreateInstance(customAttribute.FormatterType, customAttribute.Arguments);
					CS$<>8__locals1.serializeCustomFormatters.Add(obj);
				}
				else
				{
					CS$<>8__locals1.serializeCustomFormatters.Add(null);
				}
			}
			MetaMember[] members = metaType.Members;
			for (int i = 0; i < members.Length; i++)
			{
				JsonFormatterAttribute customAttribute2 = members[i].GetCustomAttribute<JsonFormatterAttribute>(true);
				if (customAttribute2 != null)
				{
					object obj2 = Activator.CreateInstance(customAttribute2.FormatterType, customAttribute2.Arguments);
					CS$<>8__locals1.deserializeCustomFormatters.Add(obj2);
				}
				else
				{
					CS$<>8__locals1.deserializeCustomFormatters.Add(null);
				}
			}
			DynamicMethod dynamicMethod = new DynamicMethod("Serialize", null, new Type[]
			{
				typeof(byte[][]),
				typeof(object[]),
				typeof(JsonWriter).MakeByRefType(),
				type,
				typeof(IJsonFormatterResolver)
			}, type.Module, true);
			ILGenerator il2 = dynamicMethod.GetILGenerator();
			DynamicObjectTypeBuilder.BuildSerialize(type, metaType, il2, delegate
			{
				il2.EmitLdarg(0);
			}, delegate(int index, MetaMember member)
			{
				if (CS$<>8__locals1.serializeCustomFormatters.Count == 0)
				{
					return false;
				}
				if (CS$<>8__locals1.serializeCustomFormatters[index] == null)
				{
					return false;
				}
				il2.EmitLdarg(1);
				il2.EmitLdc_I4(index);
				il2.Emit(OpCodes.Ldelem_Ref);
				il2.Emit(OpCodes.Castclass, CS$<>8__locals1.serializeCustomFormatters[index].GetType());
				return true;
			}, excludeNull, flag, 2);
			DynamicMethod dynamicMethod2 = new DynamicMethod("Deserialize", type, new Type[]
			{
				typeof(object[]),
				typeof(JsonReader).MakeByRefType(),
				typeof(IJsonFormatterResolver)
			}, type.Module, true);
			ILGenerator il = dynamicMethod2.GetILGenerator();
			DynamicObjectTypeBuilder.BuildDeserialize(type, metaType, il, delegate(int index, MetaMember member)
			{
				if (CS$<>8__locals1.deserializeCustomFormatters.Count == 0)
				{
					return false;
				}
				if (CS$<>8__locals1.deserializeCustomFormatters[index] == null)
				{
					return false;
				}
				il.EmitLdarg(0);
				il.EmitLdc_I4(index);
				il.Emit(OpCodes.Ldelem_Ref);
				il.Emit(OpCodes.Castclass, CS$<>8__locals1.deserializeCustomFormatters[index].GetType());
				return true;
			}, true, 1);
			object obj3 = dynamicMethod.CreateDelegate(typeof(AnonymousJsonSerializeAction<>).MakeGenericType(new Type[] { type }));
			object obj4 = dynamicMethod2.CreateDelegate(typeof(AnonymousJsonDeserializeFunc<>).MakeGenericType(new Type[] { type }));
			return Activator.CreateInstance(typeof(DynamicMethodAnonymousFormatter<>).MakeGenericType(new Type[] { type }), new object[]
			{
				list.ToArray(),
				CS$<>8__locals1.serializeCustomFormatters.ToArray(),
				CS$<>8__locals1.deserializeCustomFormatters.ToArray(),
				obj3,
				obj4
			});
		}

		private static Dictionary<MetaMember, FieldInfo> BuildConstructor(TypeBuilder builder, MetaType info, ConstructorInfo method, FieldBuilder stringByteKeysField, ILGenerator il, bool excludeNull, bool hasShouldSerialize)
		{
			il.EmitLdarg(0);
			il.Emit(OpCodes.Call, DynamicObjectTypeBuilder.EmitInfo.ObjectCtor);
			int num = info.Members.Count((MetaMember x) => x.IsReadable);
			il.EmitLdarg(0);
			il.EmitLdc_I4(num);
			il.Emit(OpCodes.Newarr, typeof(byte[]));
			int num2 = 0;
			foreach (MetaMember metaMember in info.Members.Where((MetaMember x) => x.IsReadable))
			{
				il.Emit(OpCodes.Dup);
				il.EmitLdc_I4(num2);
				il.Emit(OpCodes.Ldstr, metaMember.Name);
				if (excludeNull || hasShouldSerialize)
				{
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.GetEncodedPropertyName);
				}
				else if (num2 == 0)
				{
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.GetEncodedPropertyNameWithBeginObject);
				}
				else
				{
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator);
				}
				il.Emit(OpCodes.Stelem_Ref);
				num2++;
			}
			il.Emit(OpCodes.Stfld, stringByteKeysField);
			Dictionary<MetaMember, FieldInfo> dictionary = DynamicObjectTypeBuilder.BuildCustomFormatterField(builder, info, il);
			il.Emit(OpCodes.Ret);
			return dictionary;
		}

		private static Dictionary<MetaMember, FieldInfo> BuildCustomFormatterField(TypeBuilder builder, MetaType info, ILGenerator il)
		{
			Dictionary<MetaMember, FieldInfo> dictionary = new Dictionary<MetaMember, FieldInfo>();
			foreach (MetaMember metaMember in info.Members.Where((MetaMember x) => x.IsReadable || x.IsWritable))
			{
				JsonFormatterAttribute customAttribute = metaMember.GetCustomAttribute<JsonFormatterAttribute>(true);
				if (customAttribute != null)
				{
					FieldBuilder fieldBuilder = builder.DefineField(metaMember.Name + "_formatter", customAttribute.FormatterType, FieldAttributes.Private | FieldAttributes.InitOnly);
					int num = 52;
					LocalBuilder localBuilder = il.DeclareLocal(typeof(JsonFormatterAttribute));
					il.Emit(OpCodes.Ldtoken, info.Type);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetTypeFromHandle);
					il.Emit(OpCodes.Ldstr, metaMember.MemberName);
					il.EmitLdc_I4(num);
					if (metaMember.IsProperty)
					{
						il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.TypeGetProperty);
					}
					else
					{
						il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.TypeGetField);
					}
					il.EmitTrue();
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetCustomAttributeJsonFormatterAttribute);
					il.EmitStloc(localBuilder);
					il.EmitLoadThis();
					il.EmitLdloc(localBuilder);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonFormatterAttr.FormatterType);
					il.EmitLdloc(localBuilder);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonFormatterAttr.Arguments);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.ActivatorCreateInstance);
					il.Emit(OpCodes.Castclass, customAttribute.FormatterType);
					il.Emit(OpCodes.Stfld, fieldBuilder);
					dictionary.Add(metaMember, fieldBuilder);
				}
			}
			return dictionary;
		}

		private static void BuildSerialize(Type type, MetaType info, ILGenerator il, Action emitStringByteKeys, Func<int, MetaMember, bool> tryEmitLoadCustomFormatter, bool excludeNull, bool hasShouldSerialize, int firstArgIndex)
		{
			ArgumentField argumentField = new ArgumentField(il, firstArgIndex, false);
			ArgumentField argumentField2 = new ArgumentField(il, firstArgIndex + 1, type);
			ArgumentField argumentField3 = new ArgumentField(il, firstArgIndex + 2, false);
			TypeInfo typeInfo = type.GetTypeInfo();
			InnerExceptionMetaMember innerExceptionMetaMember = info.Members.OfType<InnerExceptionMetaMember>().FirstOrDefault<InnerExceptionMetaMember>();
			if (innerExceptionMetaMember != null)
			{
				innerExceptionMetaMember.argWriter = argumentField;
				innerExceptionMetaMember.argValue = argumentField2;
				innerExceptionMetaMember.argResolver = argumentField3;
			}
			Type type2;
			if (info.IsClass && info.BestmatchConstructor == null && DynamicObjectTypeBuilder.TryGetInterfaceEnumerableElementType(type, out type2))
			{
				Type type3 = typeof(IEnumerable<>).MakeGenericType(new Type[] { type2 });
				argumentField3.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetFormatterWithVerify.MakeGenericMethod(new Type[] { type3 }));
				argumentField.EmitLoad();
				argumentField2.EmitLoad();
				argumentField3.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Serialize(type3));
				il.Emit(OpCodes.Ret);
				return;
			}
			if (info.IsClass)
			{
				Label label = il.DefineLabel();
				argumentField2.EmitLoad();
				il.Emit(OpCodes.Brtrue_S, label);
				argumentField.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteNull);
				il.Emit(OpCodes.Ret);
				il.MarkLabel(label);
			}
			if (type == typeof(Exception))
			{
				Label label2 = il.DefineLabel();
				LocalBuilder localBuilder = il.DeclareLocal(typeof(Type));
				argumentField2.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetTypeMethod);
				il.EmitStloc(localBuilder);
				il.EmitLdloc(localBuilder);
				il.Emit(OpCodes.Ldtoken, typeof(Exception));
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetTypeFromHandle);
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.TypeEquals);
				il.Emit(OpCodes.Brtrue, label2);
				il.EmitLdloc(localBuilder);
				argumentField.EmitLoad();
				argumentField2.EmitLoad();
				argumentField3.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.NongenericSerialize);
				il.Emit(OpCodes.Ret);
				il.MarkLabel(label2);
			}
			LocalBuilder localBuilder2 = null;
			Label label3 = il.DefineLabel();
			Label[] array = null;
			if (excludeNull || hasShouldSerialize)
			{
				localBuilder2 = il.DeclareLocal(typeof(bool));
				argumentField.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteBeginObject);
				array = (from x in info.Members
					where x.IsReadable
					select x into _
					select il.DefineLabel()).ToArray<Label>();
			}
			int num = 0;
			foreach (MetaMember metaMember in info.Members.Where((MetaMember x) => x.IsReadable))
			{
				if (excludeNull || hasShouldSerialize)
				{
					il.MarkLabel(array[num]);
					if (excludeNull)
					{
						if (metaMember.Type.GetTypeInfo().IsNullable())
						{
							LocalBuilder localBuilder3 = il.DeclareLocal(metaMember.Type);
							argumentField2.EmitLoad();
							metaMember.EmitLoadValue(il);
							il.EmitStloc(localBuilder3);
							il.EmitLdloca(localBuilder3);
							il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetNullableHasValue(metaMember.Type.GetGenericArguments()[0]));
							il.Emit(OpCodes.Brfalse_S, (num < array.Length - 1) ? array[num + 1] : label3);
						}
						else if (!metaMember.Type.IsValueType && !(metaMember is StringConstantValueMetaMember))
						{
							argumentField2.EmitLoad();
							metaMember.EmitLoadValue(il);
							il.Emit(OpCodes.Brfalse_S, (num < array.Length - 1) ? array[num + 1] : label3);
						}
					}
					if (hasShouldSerialize && metaMember.ShouldSerializeMethodInfo != null)
					{
						argumentField2.EmitLoad();
						il.EmitCall(metaMember.ShouldSerializeMethodInfo);
						il.Emit(OpCodes.Brfalse_S, (num < array.Length - 1) ? array[num + 1] : label3);
					}
					Label label4 = il.DefineLabel();
					Label label5 = il.DefineLabel();
					il.EmitLdloc(localBuilder2);
					il.Emit(OpCodes.Brtrue_S, label5);
					il.EmitTrue();
					il.EmitStloc(localBuilder2);
					il.Emit(OpCodes.Br, label4);
					il.MarkLabel(label5);
					argumentField.EmitLoad();
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteValueSeparator);
					il.MarkLabel(label4);
				}
				argumentField.EmitLoad();
				emitStringByteKeys();
				il.EmitLdc_I4(num);
				il.Emit(OpCodes.Ldelem_Ref);
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteRaw);
				DynamicObjectTypeBuilder.EmitSerializeValue(typeInfo, metaMember, il, num, tryEmitLoadCustomFormatter, argumentField, argumentField2, argumentField3);
				num++;
			}
			il.MarkLabel(label3);
			if (!excludeNull && num == 0)
			{
				argumentField.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteBeginObject);
			}
			argumentField.EmitLoad();
			il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteEndObject);
			il.Emit(OpCodes.Ret);
		}

		private static void EmitSerializeValue(TypeInfo type, MetaMember member, ILGenerator il, int index, Func<int, MetaMember, bool> tryEmitLoadCustomFormatter, ArgumentField writer, ArgumentField argValue, ArgumentField argResolver)
		{
			Type type2 = member.Type;
			if (member is InnerExceptionMetaMember)
			{
				(member as InnerExceptionMetaMember).EmitSerializeDirectly(il);
				return;
			}
			if (tryEmitLoadCustomFormatter(index, member))
			{
				writer.EmitLoad();
				argValue.EmitLoad();
				member.EmitLoadValue(il);
				argResolver.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Serialize(type2));
				return;
			}
			if (DynamicObjectTypeBuilder.jsonPrimitiveTypes.Contains(type2))
			{
				writer.EmitLoad();
				argValue.EmitLoad();
				member.EmitLoadValue(il);
				il.EmitCall((from x in typeof(JsonWriter).GetTypeInfo().GetDeclaredMethods("Write" + type2.Name)
					orderby x.GetParameters().Length descending
					select x).First<MethodInfo>());
				return;
			}
			argResolver.EmitLoad();
			il.Emit(OpCodes.Call, DynamicObjectTypeBuilder.EmitInfo.GetFormatterWithVerify.MakeGenericMethod(new Type[] { type2 }));
			writer.EmitLoad();
			argValue.EmitLoad();
			member.EmitLoadValue(il);
			argResolver.EmitLoad();
			il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Serialize(type2));
		}

		private unsafe static void BuildDeserialize(Type type, MetaType info, ILGenerator il, Func<int, MetaMember, bool> tryEmitLoadCustomFormatter, bool useGetUninitializedObject, int firstArgIndex)
		{
			DynamicObjectTypeBuilder.<>c__DisplayClass12_0 CS$<>8__locals1 = new DynamicObjectTypeBuilder.<>c__DisplayClass12_0();
			CS$<>8__locals1.il = il;
			CS$<>8__locals1.tryEmitLoadCustomFormatter = tryEmitLoadCustomFormatter;
			if (info.IsClass && info.BestmatchConstructor == null && (!useGetUninitializedObject || !info.IsConcreteClass))
			{
				CS$<>8__locals1.il.Emit(OpCodes.Ldstr, "generated serializer for " + type.Name + " does not support deserialize.");
				CS$<>8__locals1.il.Emit(OpCodes.Newobj, DynamicObjectTypeBuilder.EmitInfo.InvalidOperationExceptionConstructor);
				CS$<>8__locals1.il.Emit(OpCodes.Throw);
				return;
			}
			CS$<>8__locals1.argReader = new ArgumentField(CS$<>8__locals1.il, firstArgIndex, false);
			CS$<>8__locals1.argResolver = new ArgumentField(CS$<>8__locals1.il, firstArgIndex + 1, false);
			Label label = CS$<>8__locals1.il.DefineLabel();
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsNull);
			CS$<>8__locals1.il.Emit(OpCodes.Brfalse_S, label);
			if (info.IsClass)
			{
				CS$<>8__locals1.il.Emit(OpCodes.Ldnull);
				CS$<>8__locals1.il.Emit(OpCodes.Ret);
			}
			else
			{
				CS$<>8__locals1.il.Emit(OpCodes.Ldstr, "json value is null, struct is not supported");
				CS$<>8__locals1.il.Emit(OpCodes.Newobj, DynamicObjectTypeBuilder.EmitInfo.InvalidOperationExceptionConstructor);
				CS$<>8__locals1.il.Emit(OpCodes.Throw);
			}
			CS$<>8__locals1.il.MarkLabel(label);
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsBeginObjectWithVerify);
			CS$<>8__locals1.isSideEffectFreeType = true;
			if (info.BestmatchConstructor != null)
			{
				CS$<>8__locals1.isSideEffectFreeType = DynamicObjectTypeBuilder.IsSideEffectFreeConstructorType(info.BestmatchConstructor);
				if (info.Members.Any((MetaMember x) => !x.IsReadable && x.IsWritable))
				{
					CS$<>8__locals1.isSideEffectFreeType = false;
				}
			}
			CS$<>8__locals1.infoList = info.Members.Select((MetaMember item) => new DynamicObjectTypeBuilder.DeserializeInfo
			{
				MemberInfo = item,
				LocalField = CS$<>8__locals1.il.DeclareLocal(item.Type),
				IsDeserializedField = (CS$<>8__locals1.isSideEffectFreeType ? null : CS$<>8__locals1.il.DeclareLocal(typeof(bool)))
			}).ToArray<DynamicObjectTypeBuilder.DeserializeInfo>();
			LocalBuilder localBuilder = CS$<>8__locals1.il.DeclareLocal(typeof(int));
			AutomataDictionary automataDictionary = new AutomataDictionary();
			for (int i = 0; i < info.Members.Length; i++)
			{
				automataDictionary.Add(JsonWriter.GetEncodedPropertyNameWithoutQuotation(info.Members[i].Name), i);
			}
			LocalBuilder localBuilder2 = CS$<>8__locals1.il.DeclareLocal(typeof(byte[]));
			LocalBuilder localBuilder3 = CS$<>8__locals1.il.DeclareLocal(typeof(byte).MakeByRefType(), true);
			LocalBuilder localBuilder4 = CS$<>8__locals1.il.DeclareLocal(typeof(ArraySegment<byte>));
			LocalBuilder localBuilder5 = CS$<>8__locals1.il.DeclareLocal(typeof(ulong));
			LocalBuilder localBuilder6 = CS$<>8__locals1.il.DeclareLocal(typeof(byte*));
			LocalBuilder localBuilder7 = CS$<>8__locals1.il.DeclareLocal(typeof(int));
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.GetBufferUnsafe);
			CS$<>8__locals1.il.EmitStloc(localBuilder2);
			CS$<>8__locals1.il.EmitLdloc(localBuilder2);
			CS$<>8__locals1.il.EmitLdc_I4(0);
			CS$<>8__locals1.il.Emit(OpCodes.Ldelema, typeof(byte));
			CS$<>8__locals1.il.EmitStloc(localBuilder3);
			Label continueWhile = CS$<>8__locals1.il.DefineLabel();
			Label label2 = CS$<>8__locals1.il.DefineLabel();
			Label readNext = CS$<>8__locals1.il.DefineLabel();
			CS$<>8__locals1.il.MarkLabel(continueWhile);
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitLdloca(localBuilder);
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsEndObjectWithSkipValueSeparator);
			CS$<>8__locals1.il.Emit(OpCodes.Brtrue, label2);
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadPropertyNameSegmentUnsafe);
			CS$<>8__locals1.il.EmitStloc(localBuilder4);
			CS$<>8__locals1.il.EmitLdloc(localBuilder3);
			CS$<>8__locals1.il.Emit(OpCodes.Conv_I);
			CS$<>8__locals1.il.EmitLdloca(localBuilder4);
			CS$<>8__locals1.il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Offset").GetGetMethod());
			CS$<>8__locals1.il.Emit(OpCodes.Add);
			CS$<>8__locals1.il.EmitStloc(localBuilder6);
			CS$<>8__locals1.il.EmitLdloca(localBuilder4);
			CS$<>8__locals1.il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Count").GetGetMethod());
			CS$<>8__locals1.il.EmitStloc(localBuilder7);
			CS$<>8__locals1.il.EmitLdloc(localBuilder7);
			CS$<>8__locals1.il.Emit(OpCodes.Brfalse, readNext);
			automataDictionary.EmitMatch(CS$<>8__locals1.il, localBuilder6, localBuilder7, localBuilder5, delegate(KeyValuePair<string, int> x)
			{
				int value = x.Value;
				if (CS$<>8__locals1.infoList[value].MemberInfo != null)
				{
					DynamicObjectTypeBuilder.EmitDeserializeValue(CS$<>8__locals1.il, CS$<>8__locals1.infoList[value], value, CS$<>8__locals1.tryEmitLoadCustomFormatter, CS$<>8__locals1.argReader, CS$<>8__locals1.argResolver);
					if (!CS$<>8__locals1.isSideEffectFreeType)
					{
						CS$<>8__locals1.il.EmitTrue();
						CS$<>8__locals1.il.EmitStloc(CS$<>8__locals1.infoList[value].IsDeserializedField);
					}
					CS$<>8__locals1.il.Emit(OpCodes.Br, continueWhile);
					return;
				}
				CS$<>8__locals1.il.Emit(OpCodes.Br, readNext);
			}, delegate
			{
				CS$<>8__locals1.il.Emit(OpCodes.Br, readNext);
			});
			CS$<>8__locals1.il.MarkLabel(readNext);
			CS$<>8__locals1.argReader.EmitLoad();
			CS$<>8__locals1.il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadNextBlock);
			CS$<>8__locals1.il.Emit(OpCodes.Br, continueWhile);
			CS$<>8__locals1.il.MarkLabel(label2);
			CS$<>8__locals1.il.Emit(OpCodes.Ldc_I4_0);
			CS$<>8__locals1.il.Emit(OpCodes.Conv_U);
			CS$<>8__locals1.il.EmitStloc(localBuilder3);
			LocalBuilder localBuilder8 = DynamicObjectTypeBuilder.EmitNewObject(CS$<>8__locals1.il, type, info, CS$<>8__locals1.infoList, CS$<>8__locals1.isSideEffectFreeType);
			if (localBuilder8 != null)
			{
				CS$<>8__locals1.il.Emit(OpCodes.Ldloc, localBuilder8);
			}
			CS$<>8__locals1.il.Emit(OpCodes.Ret);
		}

		private static void EmitDeserializeValue(ILGenerator il, DynamicObjectTypeBuilder.DeserializeInfo info, int index, Func<int, MetaMember, bool> tryEmitLoadCustomFormatter, ArgumentField reader, ArgumentField argResolver)
		{
			MetaMember memberInfo = info.MemberInfo;
			Type type = memberInfo.Type;
			if (tryEmitLoadCustomFormatter(index, memberInfo))
			{
				reader.EmitLoad();
				argResolver.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Deserialize(type));
			}
			else if (DynamicObjectTypeBuilder.jsonPrimitiveTypes.Contains(type))
			{
				reader.EmitLoad();
				il.EmitCall((from x in typeof(JsonReader).GetTypeInfo().GetDeclaredMethods("Read" + type.Name)
					orderby x.GetParameters().Length descending
					select x).First<MethodInfo>());
			}
			else
			{
				argResolver.EmitLoad();
				il.Emit(OpCodes.Call, DynamicObjectTypeBuilder.EmitInfo.GetFormatterWithVerify.MakeGenericMethod(new Type[] { type }));
				reader.EmitLoad();
				argResolver.EmitLoad();
				il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.Deserialize(type));
			}
			il.EmitStloc(info.LocalField);
		}

		private static LocalBuilder EmitNewObject(ILGenerator il, Type type, MetaType info, DynamicObjectTypeBuilder.DeserializeInfo[] members, bool isSideEffectFreeType)
		{
			if (info.IsClass)
			{
				LocalBuilder localBuilder = null;
				if (!isSideEffectFreeType)
				{
					localBuilder = il.DeclareLocal(type);
				}
				if (info.BestmatchConstructor != null)
				{
					MetaMember[] array = info.ConstructorParameters;
					for (int i = 0; i < array.Length; i++)
					{
						MetaMember item2 = array[i];
						DynamicObjectTypeBuilder.DeserializeInfo deserializeInfo = members.First((DynamicObjectTypeBuilder.DeserializeInfo x) => x.MemberInfo == item2);
						il.EmitLdloc(deserializeInfo.LocalField);
					}
					il.Emit(OpCodes.Newobj, info.BestmatchConstructor);
				}
				else
				{
					il.Emit(OpCodes.Ldtoken, type);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetTypeFromHandle);
					il.EmitCall(DynamicObjectTypeBuilder.EmitInfo.GetUninitializedObject);
				}
				if (!isSideEffectFreeType)
				{
					il.EmitStloc(localBuilder);
				}
				foreach (DynamicObjectTypeBuilder.DeserializeInfo deserializeInfo2 in members.Where((DynamicObjectTypeBuilder.DeserializeInfo x) => x.MemberInfo != null && x.MemberInfo.IsWritable))
				{
					if (isSideEffectFreeType)
					{
						il.Emit(OpCodes.Dup);
						il.EmitLdloc(deserializeInfo2.LocalField);
						deserializeInfo2.MemberInfo.EmitStoreValue(il);
					}
					else
					{
						Label label = il.DefineLabel();
						il.EmitLdloc(deserializeInfo2.IsDeserializedField);
						il.Emit(OpCodes.Brfalse, label);
						il.EmitLdloc(localBuilder);
						il.EmitLdloc(deserializeInfo2.LocalField);
						deserializeInfo2.MemberInfo.EmitStoreValue(il);
						il.MarkLabel(label);
					}
				}
				return localBuilder;
			}
			LocalBuilder localBuilder2 = il.DeclareLocal(type);
			if (info.BestmatchConstructor == null)
			{
				il.Emit(OpCodes.Ldloca, localBuilder2);
				il.Emit(OpCodes.Initobj, type);
			}
			else
			{
				MetaMember[] array = info.ConstructorParameters;
				for (int i = 0; i < array.Length; i++)
				{
					MetaMember item = array[i];
					DynamicObjectTypeBuilder.DeserializeInfo deserializeInfo3 = members.First((DynamicObjectTypeBuilder.DeserializeInfo x) => x.MemberInfo == item);
					il.EmitLdloc(deserializeInfo3.LocalField);
				}
				il.Emit(OpCodes.Newobj, info.BestmatchConstructor);
				il.Emit(OpCodes.Stloc, localBuilder2);
			}
			foreach (DynamicObjectTypeBuilder.DeserializeInfo deserializeInfo4 in members.Where((DynamicObjectTypeBuilder.DeserializeInfo x) => x.MemberInfo != null && x.MemberInfo.IsWritable))
			{
				if (isSideEffectFreeType)
				{
					il.EmitLdloca(localBuilder2);
					il.EmitLdloc(deserializeInfo4.LocalField);
					deserializeInfo4.MemberInfo.EmitStoreValue(il);
				}
				else
				{
					Label label2 = il.DefineLabel();
					il.EmitLdloc(deserializeInfo4.IsDeserializedField);
					il.Emit(OpCodes.Brfalse, label2);
					il.EmitLdloca(localBuilder2);
					il.EmitLdloc(deserializeInfo4.LocalField);
					deserializeInfo4.MemberInfo.EmitStoreValue(il);
					il.MarkLabel(label2);
				}
			}
			return localBuilder2;
		}

		private static bool IsSideEffectFreeConstructorType(ConstructorInfo ctorInfo)
		{
			MethodBody methodBody = ctorInfo.GetMethodBody();
			if (methodBody == null)
			{
				return false;
			}
			byte[] ilasByteArray = methodBody.GetILAsByteArray();
			if (ilasByteArray == null)
			{
				return false;
			}
			List<OpCode> list = new List<OpCode>();
			using (ILStreamReader ilstreamReader = new ILStreamReader(ilasByteArray))
			{
				while (!ilstreamReader.EndOfStream)
				{
					OpCode opCode = ilstreamReader.ReadOpCode();
					if (opCode != OpCodes.Nop && opCode != OpCodes.Ldloc_0 && opCode != OpCodes.Ldloc_S && opCode != OpCodes.Stloc_0 && opCode != OpCodes.Stloc_S && opCode != OpCodes.Blt && opCode != OpCodes.Blt_S && opCode != OpCodes.Bgt && opCode != OpCodes.Bgt_S)
					{
						list.Add(opCode);
						if (list.Count == 4)
						{
							break;
						}
					}
				}
			}
			if (list.Count != 3 || !(list[0] == OpCodes.Ldarg_0) || !(list[1] == OpCodes.Call) || !(list[2] == OpCodes.Ret))
			{
				return false;
			}
			if (ctorInfo.DeclaringType.BaseType == typeof(object))
			{
				return true;
			}
			ConstructorInfo constructor = ctorInfo.DeclaringType.BaseType.GetConstructor(Type.EmptyTypes);
			return !(constructor == null) && DynamicObjectTypeBuilder.IsSideEffectFreeConstructorType(constructor);
		}

		private static bool TryGetInterfaceEnumerableElementType(Type type, out Type elementType)
		{
			foreach (Type type2 in type.GetInterfaces())
			{
				if (type2.IsGenericType && type2.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					Type[] genericArguments = type2.GetGenericArguments();
					elementType = genericArguments[0];
					return true;
				}
			}
			elementType = null;
			return false;
		}

		private static readonly Regex SubtractFullNameRegex = new Regex(", Version=\\d+.\\d+.\\d+.\\d+, Culture=\\w+, PublicKeyToken=\\w+");

		private static int nameSequence = 0;

		private static HashSet<Type> ignoreTypes = new HashSet<Type>
		{
			typeof(object),
			typeof(short),
			typeof(int),
			typeof(long),
			typeof(ushort),
			typeof(uint),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(bool),
			typeof(byte),
			typeof(sbyte),
			typeof(decimal),
			typeof(char),
			typeof(string),
			typeof(Guid),
			typeof(TimeSpan),
			typeof(DateTime),
			typeof(DateTimeOffset)
		};

		private static HashSet<Type> jsonPrimitiveTypes = new HashSet<Type>
		{
			typeof(short),
			typeof(int),
			typeof(long),
			typeof(ushort),
			typeof(uint),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(bool),
			typeof(byte),
			typeof(sbyte),
			typeof(string)
		};

		private struct DeserializeInfo
		{
			public MetaMember MemberInfo;

			public LocalBuilder LocalField;

			public LocalBuilder IsDeserializedField;
		}

		internal static class EmitInfo
		{
			public static MethodInfo Serialize(Type type)
			{
				return typeof(IJsonFormatter<>).MakeGenericType(new Type[] { type }).GetRuntimeMethod("Serialize", new Type[]
				{
					typeof(Utf8Json.JsonWriter).MakeByRefType(),
					type,
					typeof(IJsonFormatterResolver)
				});
			}

			public static MethodInfo Deserialize(Type type)
			{
				return typeof(IJsonFormatter<>).MakeGenericType(new Type[] { type }).GetRuntimeMethod("Deserialize", new Type[]
				{
					typeof(Utf8Json.JsonReader).MakeByRefType(),
					typeof(IJsonFormatterResolver)
				});
			}

			public static MethodInfo GetNullableHasValue(Type type)
			{
				return typeof(Nullable<>).MakeGenericType(new Type[] { type }).GetRuntimeProperty("HasValue").GetGetMethod();
			}

			// Note: this type is marked as 'beforefieldinit'.
			static EmitInfo()
			{
				ParameterExpression parameterExpression;
				DynamicObjectTypeBuilder.EmitInfo.GetTypeMethod = ExpressionUtility.GetMethodInfo<object, Type>(Expression.Lambda<Func<object, Type>>(Expression.Call(parameterExpression, methodof(object.GetType()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
				DynamicObjectTypeBuilder.EmitInfo.TypeEquals = ExpressionUtility.GetMethodInfo<Type, bool>((Type t) => t.Equals(null));
				DynamicObjectTypeBuilder.EmitInfo.NongenericSerialize = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>((Utf8Json.JsonWriter writer) => JsonSerializer.NonGeneric.Serialize(null, writer, null, null));
			}

			public static readonly ConstructorInfo ObjectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.First((ConstructorInfo x) => x.GetParameters().Length == 0);

			public static readonly MethodInfo GetFormatterWithVerify = typeof(JsonFormatterResolverExtensions).GetRuntimeMethod("GetFormatterWithVerify", new Type[] { typeof(IJsonFormatterResolver) });

			public static readonly ConstructorInfo InvalidOperationExceptionConstructor = typeof(InvalidOperationException).GetTypeInfo().DeclaredConstructors.First(delegate(ConstructorInfo x)
			{
				ParameterInfo[] parameters = x.GetParameters();
				return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
			});

			public static readonly MethodInfo GetTypeFromHandle = ExpressionUtility.GetMethodInfo<Type>(Expression.Lambda<Func<Type>>(Expression.Call(null, methodof(Type.GetTypeFromHandle(RuntimeTypeHandle)), new Expression[] { Expression.Constant(default(RuntimeTypeHandle), typeof(RuntimeTypeHandle)) }), Array.Empty<ParameterExpression>()));

			public static readonly MethodInfo TypeGetProperty = ExpressionUtility.GetMethodInfo<Type, PropertyInfo>((Type t) => t.GetProperty(null, BindingFlags.Default));

			public static readonly MethodInfo TypeGetField = ExpressionUtility.GetMethodInfo<Type, FieldInfo>((Type t) => t.GetField(null, BindingFlags.Default));

			public static readonly MethodInfo GetCustomAttributeJsonFormatterAttribute = ExpressionUtility.GetMethodInfo<JsonFormatterAttribute>(Expression.Lambda<Func<JsonFormatterAttribute>>(Expression.Call(null, methodof(MemberInfo.GetCustomAttribute(bool)), new Expression[]
			{
				Expression.Constant(null, typeof(MemberInfo)),
				Expression.Constant(false, typeof(bool))
			}), Array.Empty<ParameterExpression>()));

			public static readonly MethodInfo ActivatorCreateInstance = ExpressionUtility.GetMethodInfo<object>(Expression.Lambda<Func<object>>(Expression.Call(null, methodof(Activator.CreateInstance(Type, object[])), new Expression[]
			{
				Expression.Constant(null, typeof(Type)),
				Expression.Constant(null, typeof(object[]))
			}), Array.Empty<ParameterExpression>()));

			public static readonly MethodInfo GetUninitializedObject = ExpressionUtility.GetMethodInfo<object>(Expression.Lambda<Func<object>>(Expression.Call(null, methodof(FormatterServices.GetUninitializedObject(Type)), new Expression[] { Expression.Constant(null, typeof(Type)) }), Array.Empty<ParameterExpression>()));

			public static readonly MethodInfo GetTypeMethod;

			public static readonly MethodInfo TypeEquals;

			public static readonly MethodInfo NongenericSerialize;

			internal static class JsonWriter
			{
				static JsonWriter()
				{
					ParameterExpression parameterExpression;
					DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteNull = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>(Expression.Lambda<Action<Utf8Json.JsonWriter>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonWriter.WriteNull()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
					DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteRaw = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>((Utf8Json.JsonWriter writer) => writer.WriteRaw(null));
					DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteBeginObject = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>(Expression.Lambda<Action<Utf8Json.JsonWriter>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonWriter.WriteBeginObject()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
					DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteEndObject = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>(Expression.Lambda<Action<Utf8Json.JsonWriter>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonWriter.WriteEndObject()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
					DynamicObjectTypeBuilder.EmitInfo.JsonWriter.WriteValueSeparator = ExpressionUtility.GetMethodInfo<Utf8Json.JsonWriter>(Expression.Lambda<Action<Utf8Json.JsonWriter>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonWriter.WriteValueSeparator()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
				}

				public static readonly MethodInfo GetEncodedPropertyNameWithBeginObject = ExpressionUtility.GetMethodInfo<byte[]>(Expression.Lambda<Func<byte[]>>(Expression.Call(null, methodof(Utf8Json.JsonWriter.GetEncodedPropertyNameWithBeginObject(string)), new Expression[] { Expression.Constant(null, typeof(string)) }), Array.Empty<ParameterExpression>()));

				public static readonly MethodInfo GetEncodedPropertyNameWithPrefixValueSeparator = ExpressionUtility.GetMethodInfo<byte[]>(Expression.Lambda<Func<byte[]>>(Expression.Call(null, methodof(Utf8Json.JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator(string)), new Expression[] { Expression.Constant(null, typeof(string)) }), Array.Empty<ParameterExpression>()));

				public static readonly MethodInfo GetEncodedPropertyNameWithoutQuotation = ExpressionUtility.GetMethodInfo<byte[]>(Expression.Lambda<Func<byte[]>>(Expression.Call(null, methodof(Utf8Json.JsonWriter.GetEncodedPropertyNameWithoutQuotation(string)), new Expression[] { Expression.Constant(null, typeof(string)) }), Array.Empty<ParameterExpression>()));

				public static readonly MethodInfo GetEncodedPropertyName = ExpressionUtility.GetMethodInfo<byte[]>(Expression.Lambda<Func<byte[]>>(Expression.Call(null, methodof(Utf8Json.JsonWriter.GetEncodedPropertyName(string)), new Expression[] { Expression.Constant(null, typeof(string)) }), Array.Empty<ParameterExpression>()));

				public static readonly MethodInfo WriteNull;

				public static readonly MethodInfo WriteRaw;

				public static readonly MethodInfo WriteBeginObject;

				public static readonly MethodInfo WriteEndObject;

				public static readonly MethodInfo WriteValueSeparator;
			}

			internal static class JsonReader
			{
				static JsonReader()
				{
					ParameterExpression parameterExpression;
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsNull = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader, bool>(Expression.Lambda<Func<Utf8Json.JsonReader, bool>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonReader.ReadIsNull()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsBeginObjectWithVerify = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader>(Expression.Lambda<Action<Utf8Json.JsonReader>>(Expression.Call(parameterExpression, methodof(Utf8Json.JsonReader.ReadIsBeginObjectWithVerify()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression }));
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadIsEndObjectWithSkipValueSeparator = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader, int, bool>((Utf8Json.JsonReader reader, int count) => reader.ReadIsEndObjectWithSkipValueSeparator(count));
					ParameterExpression parameterExpression2;
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadPropertyNameSegmentUnsafe = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader, ArraySegment<byte>>(Expression.Lambda<Func<Utf8Json.JsonReader, ArraySegment<byte>>>(Expression.Call(parameterExpression2, methodof(Utf8Json.JsonReader.ReadPropertyNameSegmentRaw()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression2 }));
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.ReadNextBlock = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader>(Expression.Lambda<Action<Utf8Json.JsonReader>>(Expression.Call(parameterExpression2, methodof(Utf8Json.JsonReader.ReadNextBlock()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression2 }));
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.GetBufferUnsafe = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader, byte[]>(Expression.Lambda<Func<Utf8Json.JsonReader, byte[]>>(Expression.Call(parameterExpression2, methodof(Utf8Json.JsonReader.GetBufferUnsafe()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression2 }));
					DynamicObjectTypeBuilder.EmitInfo.JsonReader.GetCurrentOffsetUnsafe = ExpressionUtility.GetMethodInfo<Utf8Json.JsonReader, int>(Expression.Lambda<Func<Utf8Json.JsonReader, int>>(Expression.Call(parameterExpression2, methodof(Utf8Json.JsonReader.GetCurrentOffsetUnsafe()), Array.Empty<Expression>()), new ParameterExpression[] { parameterExpression2 }));
				}

				public static readonly MethodInfo ReadIsNull;

				public static readonly MethodInfo ReadIsBeginObjectWithVerify;

				public static readonly MethodInfo ReadIsEndObjectWithSkipValueSeparator;

				public static readonly MethodInfo ReadPropertyNameSegmentUnsafe;

				public static readonly MethodInfo ReadNextBlock;

				public static readonly MethodInfo GetBufferUnsafe;

				public static readonly MethodInfo GetCurrentOffsetUnsafe;
			}

			internal static class JsonFormatterAttr
			{
				internal static readonly MethodInfo FormatterType = ExpressionUtility.GetPropertyInfo<JsonFormatterAttribute, Type>((JsonFormatterAttribute attr) => attr.FormatterType).GetGetMethod();

				internal static readonly MethodInfo Arguments = ExpressionUtility.GetPropertyInfo<JsonFormatterAttribute, object[]>((JsonFormatterAttribute attr) => attr.Arguments).GetGetMethod();
			}
		}

		internal class Utf8JsonDynamicObjectResolverException : Exception
		{
			public Utf8JsonDynamicObjectResolverException(string message)
				: base(message)
			{
			}
		}
	}
}
