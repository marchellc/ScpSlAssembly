using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit;

internal class MetaMember
{
	private MethodInfo getMethod;

	private MethodInfo setMethod;

	public string Name { get; private set; }

	public string MemberName { get; private set; }

	public bool IsProperty => this.PropertyInfo != null;

	public bool IsField => this.FieldInfo != null;

	public bool IsWritable { get; private set; }

	public bool IsReadable { get; private set; }

	public Type Type { get; private set; }

	public FieldInfo FieldInfo { get; private set; }

	public PropertyInfo PropertyInfo { get; private set; }

	public MethodInfo ShouldSerializeMethodInfo { get; private set; }

	protected MetaMember(Type type, string name, string memberName, bool isWritable, bool isReadable)
	{
		this.Name = name;
		this.MemberName = memberName;
		this.Type = type;
		this.IsWritable = isWritable;
		this.IsReadable = isReadable;
	}

	public MetaMember(FieldInfo info, string name, bool allowPrivate)
	{
		this.Name = name;
		this.MemberName = info.Name;
		this.FieldInfo = info;
		this.Type = info.FieldType;
		this.IsReadable = allowPrivate || info.IsPublic;
		this.IsWritable = allowPrivate || (info.IsPublic && !info.IsInitOnly);
		this.ShouldSerializeMethodInfo = MetaMember.GetShouldSerialize(info);
	}

	public MetaMember(PropertyInfo info, string name, bool allowPrivate)
	{
		this.getMethod = info.GetGetMethod(nonPublic: true);
		this.setMethod = info.GetSetMethod(nonPublic: true);
		this.Name = name;
		this.MemberName = info.Name;
		this.PropertyInfo = info;
		this.Type = info.PropertyType;
		this.IsReadable = this.getMethod != null && (allowPrivate || this.getMethod.IsPublic) && !this.getMethod.IsStatic;
		this.IsWritable = this.setMethod != null && (allowPrivate || this.setMethod.IsPublic) && !this.setMethod.IsStatic;
		this.ShouldSerializeMethodInfo = MetaMember.GetShouldSerialize(info);
	}

	private static MethodInfo GetShouldSerialize(MemberInfo info)
	{
		string shouldSerialize = "ShouldSerialize" + info.Name;
		return (from x in info.DeclaringType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
			where x.Name == shouldSerialize && x.ReturnType == typeof(bool) && x.GetParameters().Length == 0
			select x).FirstOrDefault();
	}

	public T GetCustomAttribute<T>(bool inherit) where T : Attribute
	{
		if (this.IsProperty)
		{
			return this.PropertyInfo.GetCustomAttribute<T>(inherit);
		}
		if (this.FieldInfo != null)
		{
			return this.FieldInfo.GetCustomAttribute<T>(inherit);
		}
		return null;
	}

	public virtual void EmitLoadValue(ILGenerator il)
	{
		if (this.IsProperty)
		{
			il.EmitCall(this.getMethod);
		}
		else
		{
			il.Emit(OpCodes.Ldfld, this.FieldInfo);
		}
	}

	public virtual void EmitStoreValue(ILGenerator il)
	{
		if (this.IsProperty)
		{
			il.EmitCall(this.setMethod);
		}
		else
		{
			il.Emit(OpCodes.Stfld, this.FieldInfo);
		}
	}
}
