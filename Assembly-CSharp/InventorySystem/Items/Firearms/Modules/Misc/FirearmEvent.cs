using System;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	[Serializable]
	public class FirearmEvent
	{
		public static FirearmEvent CurrentlyInvokedEvent { get; private set; }

		public EventInvocationDetails LastInvocation { get; private set; }

		public void InvokeSafe(EventInvocationDetails data)
		{
			try
			{
				FirearmEvent.CurrentlyInvokedEvent = this;
				this.LastInvocation = data;
				this.Action.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				FirearmEvent.CurrentlyInvokedEvent = null;
			}
		}

		public UnityEvent Action;

		public AnimationClip Clip;

		public float Frame;

		public int NameHash;

		public float LengthFrames;
	}
}
