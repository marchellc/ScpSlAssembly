using System;
using System.Reflection.Emit;

namespace Utf8Json.Internal.Emit
{
	internal class StringConstantValueMetaMember : MetaMember
	{
		public StringConstantValueMetaMember(string name, string constant)
			: base(typeof(string), name, name, false, true)
		{
			this.constant = constant;
		}

		public override void EmitLoadValue(ILGenerator il)
		{
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ldstr, this.constant);
		}

		public override void EmitStoreValue(ILGenerator il)
		{
			throw new NotSupportedException();
		}

		private readonly string constant;
	}
}
