using System;

namespace Interactables.Interobjects.DoorUtils
{
	public static class DoorEvents
	{
		public static event Action<DoorVariant, DoorAction, ReferenceHub> OnDoorAction;

		public static void TriggerAction(DoorVariant variant, DoorAction action, ReferenceHub user)
		{
			DoorEvents.OnDoorAction(variant, action, user);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static DoorEvents()
		{
			DoorEvents.OnDoorAction = delegate(DoorVariant variant, DoorAction action, ReferenceHub user)
			{
			};
		}
	}
}
