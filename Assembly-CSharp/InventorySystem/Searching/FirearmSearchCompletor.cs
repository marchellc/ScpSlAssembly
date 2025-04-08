using System;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Extensions;
using UnityEngine;

namespace InventorySystem.Searching
{
	public class FirearmSearchCompletor : ItemSearchCompletor
	{
		public FirearmSearchCompletor(ReferenceHub hub, FirearmPickup targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
			this._extensions = targetPickup.Worldmodel.Extensions;
		}

		protected override bool ValidateAny()
		{
			if (!base.ValidateAny())
			{
				return false;
			}
			Component[] extensions = this._extensions;
			for (int i = 0; i < extensions.Length; i++)
			{
				IPickupLockingExtension pickupLockingExtension = extensions[i] as IPickupLockingExtension;
				if (pickupLockingExtension != null && pickupLockingExtension.LockPrefab)
				{
					return false;
				}
			}
			return true;
		}

		private readonly Component[] _extensions;
	}
}
