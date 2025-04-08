using System;
using System.Reflection;
using System.Reflection.Emit;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Resolvers
{
	public abstract class DynamicCompositeResolver : IJsonFormatterResolver
	{
		public static IJsonFormatterResolver Create(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
		{
			string text = Guid.NewGuid().ToString().Replace("-", "");
			TypeBuilder typeBuilder = DynamicCompositeResolver.assembly.DefineType("DynamicCompositeResolver_" + text, TypeAttributes.Public | TypeAttributes.Sealed, typeof(DynamicCompositeResolver));
			TypeBuilder typeBuilder2 = DynamicCompositeResolver.assembly.DefineType("DynamicCompositeResolverCache_" + text, TypeAttributes.Public | TypeAttributes.Sealed, null);
			GenericTypeParameterBuilder genericTypeParameterBuilder = typeBuilder2.DefineGenericParameters(new string[] { "T" })[0];
			FieldBuilder fieldBuilder = typeBuilder.DefineField("instance", typeBuilder, FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
			FieldBuilder fieldBuilder2 = typeBuilder2.DefineField("formatter", typeof(IJsonFormatter<>).MakeGenericType(new Type[] { genericTypeParameterBuilder }), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
			ILGenerator ilgenerator = typeBuilder2.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes).GetILGenerator();
			ilgenerator.EmitLdsfld(fieldBuilder);
			ilgenerator.EmitCall(typeof(DynamicCompositeResolver).GetMethod("GetFormatterLoop").MakeGenericMethod(new Type[] { genericTypeParameterBuilder }));
			ilgenerator.Emit(OpCodes.Stsfld, fieldBuilder2);
			ilgenerator.Emit(OpCodes.Ret);
			Type type = typeBuilder2.CreateTypeInfo().AsType();
			ILGenerator ilgenerator2 = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[]
			{
				typeof(IJsonFormatter[]),
				typeof(IJsonFormatterResolver[])
			}).GetILGenerator();
			ilgenerator2.EmitLdarg(0);
			ilgenerator2.EmitLdarg(1);
			ilgenerator2.EmitLdarg(2);
			ilgenerator2.Emit(OpCodes.Call, typeof(DynamicCompositeResolver).GetConstructors()[0]);
			ilgenerator2.Emit(OpCodes.Ret);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("GetFormatter", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual);
			GenericTypeParameterBuilder genericTypeParameterBuilder2 = methodBuilder.DefineGenericParameters(new string[] { "T" })[0];
			methodBuilder.SetReturnType(typeof(IJsonFormatter<>).MakeGenericType(new Type[] { genericTypeParameterBuilder2 }));
			ILGenerator ilgenerator3 = methodBuilder.GetILGenerator();
			FieldInfo field = TypeBuilder.GetField(type.MakeGenericType(new Type[] { genericTypeParameterBuilder2 }), type.GetField("formatter", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField));
			ilgenerator3.EmitLdsfld(field);
			ilgenerator3.Emit(OpCodes.Ret);
			object obj = Activator.CreateInstance(typeBuilder.CreateTypeInfo().AsType(), new object[] { formatters, resolvers });
			obj.GetType().GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, obj);
			return (IJsonFormatterResolver)obj;
		}

		public DynamicCompositeResolver(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
		{
			this.formatters = formatters;
			this.resolvers = resolvers;
		}

		public IJsonFormatter<T> GetFormatterLoop<T>()
		{
			foreach (IJsonFormatter jsonFormatter in this.formatters)
			{
				foreach (Type type in jsonFormatter.GetType().GetTypeInfo().ImplementedInterfaces)
				{
					TypeInfo typeInfo = type.GetTypeInfo();
					if (typeInfo.IsGenericType && typeInfo.GenericTypeArguments[0] == typeof(T))
					{
						return (IJsonFormatter<T>)jsonFormatter;
					}
				}
			}
			IJsonFormatterResolver[] array2 = this.resolvers;
			for (int i = 0; i < array2.Length; i++)
			{
				IJsonFormatter<T> formatter = array2[i].GetFormatter<T>();
				if (formatter != null)
				{
					return formatter;
				}
			}
			return null;
		}

		public abstract IJsonFormatter<T> GetFormatter<T>();

		private const string ModuleName = "Utf8Json.Resolvers.DynamicCompositeResolver";

		private static readonly DynamicAssembly assembly = new DynamicAssembly("Utf8Json.Resolvers.DynamicCompositeResolver");

		public readonly IJsonFormatter[] formatters;

		public readonly IJsonFormatterResolver[] resolvers;
	}
}
