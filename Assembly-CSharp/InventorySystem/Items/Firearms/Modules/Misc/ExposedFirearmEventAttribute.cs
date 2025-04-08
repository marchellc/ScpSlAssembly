using System;
using UnityEngine.Events;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ExposedFirearmEventAttribute : Attribute
	{
		public ExposedFirearmEventAttribute()
			: this(UnityEventCallState.RuntimeOnly)
		{
		}

		public ExposedFirearmEventAttribute(UnityEventCallState callState)
		{
			this.CallState = callState;
		}

		public readonly UnityEventCallState CallState;
	}
}
