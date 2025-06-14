using System;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit;

internal struct ArgumentField
{
	private readonly int i;

	private readonly bool @ref;

	private readonly ILGenerator il;

	public ArgumentField(ILGenerator il, int i, bool @ref = false)
	{
		this.il = il;
		this.i = i;
		this.@ref = @ref;
	}

	public ArgumentField(ILGenerator il, int i, Type type)
	{
		this.il = il;
		this.i = i;
		this.@ref = ((!type.IsClass && !type.IsInterface && !type.IsAbstract) ? true : false);
	}

	public void EmitLoad()
	{
		if (this.@ref)
		{
			this.il.EmitLdarga(this.i);
		}
		else
		{
			this.il.EmitLdarg(this.i);
		}
	}

	public void EmitStore()
	{
		this.il.EmitStarg(this.i);
	}
}
