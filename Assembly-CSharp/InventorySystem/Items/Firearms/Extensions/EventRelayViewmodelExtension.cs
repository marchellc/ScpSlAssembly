using System;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class EventRelayViewmodelExtension : MonoBehaviour, IViewmodelExtension
	{
		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			EventManagerModule eventManagerModule;
			viewmodel.ParentFirearm.TryGetModule(out eventManagerModule, true);
			eventManagerModule.OnEventRelayed += this.OnTriggered;
		}

		private void OnTriggered(int guid)
		{
			foreach (EventRelayViewmodelExtension.Relay relay in this._relays)
			{
				if (relay.GUID == guid)
				{
					relay.Action.Invoke();
				}
			}
		}

		[SerializeField]
		private EventRelayViewmodelExtension.Relay[] _relays;

		[Serializable]
		private class Relay
		{
			public int GUID;

			public UnityEvent Action;
		}
	}
}
