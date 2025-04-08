using System;

namespace InventorySystem.Items
{
	public interface ILightEmittingItem
	{
		bool IsEmittingLight { get; }
	}
}
