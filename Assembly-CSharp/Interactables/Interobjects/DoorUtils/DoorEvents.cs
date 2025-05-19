using System;

namespace Interactables.Interobjects.DoorUtils;

public static class DoorEvents
{
	public static event Action<DoorVariant, DoorAction, ReferenceHub> OnDoorAction;

	public static void TriggerAction(DoorVariant variant, DoorAction action, ReferenceHub user)
	{
		DoorEvents.OnDoorAction(variant, action, user);
	}

	static DoorEvents()
	{
		DoorEvents.OnDoorAction = delegate
		{
		};
	}
}
