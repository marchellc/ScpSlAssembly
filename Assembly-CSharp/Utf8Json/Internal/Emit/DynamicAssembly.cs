using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit;

internal class DynamicAssembly
{
	private readonly AssemblyBuilder assemblyBuilder;

	private readonly ModuleBuilder moduleBuilder;

	private readonly object gate = new object();

	public TypeBuilder DefineType(string name, TypeAttributes attr)
	{
		lock (this.gate)
		{
			return this.moduleBuilder.DefineType(name, attr);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent)
	{
		lock (this.gate)
		{
			return this.moduleBuilder.DefineType(name, attr, parent);
		}
	}

	public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
	{
		lock (this.gate)
		{
			return this.moduleBuilder.DefineType(name, attr, parent, interfaces);
		}
	}

	public DynamicAssembly(string moduleName)
	{
		this.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.Run);
		this.moduleBuilder = this.assemblyBuilder.DefineDynamicModule(moduleName);
	}
}
