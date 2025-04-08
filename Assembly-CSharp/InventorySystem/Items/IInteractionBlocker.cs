using System;

namespace InventorySystem.Items
{
	public interface IInteractionBlocker
	{
		BlockedInteraction BlockedInteractions { get; }

		bool CanBeCleared { get; }
	}
}
