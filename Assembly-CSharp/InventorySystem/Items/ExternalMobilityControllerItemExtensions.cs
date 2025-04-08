using System;

namespace InventorySystem.Items
{
	public static class ExternalMobilityControllerItemExtensions
	{
		public static object GetMobilityController(this ItemBase item)
		{
			if (item == null)
			{
				return null;
			}
			IExternalMobilityControllerItem externalMobilityControllerItem = item as IExternalMobilityControllerItem;
			if (externalMobilityControllerItem != null)
			{
				return externalMobilityControllerItem.DesignatedMobilityControllerClass;
			}
			return item;
		}
	}
}
