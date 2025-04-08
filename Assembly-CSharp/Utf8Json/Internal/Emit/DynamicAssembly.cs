using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit
{
	internal class DynamicAssembly
	{
		public TypeBuilder DefineType(string name, TypeAttributes attr)
		{
			object obj = this.gate;
			TypeBuilder typeBuilder;
			lock (obj)
			{
				typeBuilder = this.moduleBuilder.DefineType(name, attr);
			}
			return typeBuilder;
		}

		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent)
		{
			object obj = this.gate;
			TypeBuilder typeBuilder;
			lock (obj)
			{
				typeBuilder = this.moduleBuilder.DefineType(name, attr, parent);
			}
			return typeBuilder;
		}

		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
		{
			object obj = this.gate;
			TypeBuilder typeBuilder;
			lock (obj)
			{
				typeBuilder = this.moduleBuilder.DefineType(name, attr, parent, interfaces);
			}
			return typeBuilder;
		}

		public DynamicAssembly(string moduleName)
		{
			this.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.Run);
			this.moduleBuilder = this.assemblyBuilder.DefineDynamicModule(moduleName);
		}

		private readonly AssemblyBuilder assemblyBuilder;

		private readonly ModuleBuilder moduleBuilder;

		private readonly object gate = new object();
	}
}
