using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit;

internal class InnerExceptionMetaMember : MetaMember
{
	private static readonly MethodInfo getInnerException = ExpressionUtility.GetPropertyInfo((Exception ex) => ex.InnerException).GetGetMethod();

	private static readonly MethodInfo nongenericSerialize = ExpressionUtility.GetMethodInfo((JsonWriter writer) => JsonSerializer.NonGeneric.Serialize(ref writer, null, null));

	internal ArgumentField argWriter;

	internal ArgumentField argValue;

	internal ArgumentField argResolver;

	public InnerExceptionMetaMember(string name)
		: base(typeof(Exception), name, name, isWritable: false, isReadable: true)
	{
	}

	public override void EmitLoadValue(ILGenerator il)
	{
		il.Emit(OpCodes.Callvirt, InnerExceptionMetaMember.getInnerException);
	}

	public override void EmitStoreValue(ILGenerator il)
	{
		throw new NotSupportedException();
	}

	public void EmitSerializeDirectly(ILGenerator il)
	{
		this.argWriter.EmitLoad();
		this.argValue.EmitLoad();
		il.Emit(OpCodes.Callvirt, InnerExceptionMetaMember.getInnerException);
		this.argResolver.EmitLoad();
		il.EmitCall(InnerExceptionMetaMember.nongenericSerialize);
	}
}
