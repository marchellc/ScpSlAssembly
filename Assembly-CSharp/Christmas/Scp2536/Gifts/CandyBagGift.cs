using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp330;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Christmas.Scp2536.Gifts
{
	public class CandyBagGift : Scp2536GiftBase
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Two;
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (!base.CanBeGranted(hub))
			{
				return false;
			}
			return !hub.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> i) => i.Value is Scp330Bag);
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			for (int i = 0; i < 6; i++)
			{
				CandyKindID candyKindID = ((5f < global::UnityEngine.Random.Range(0f, 100f)) ? Scp330Candies.GetRandom(CandyKindID.None) : CandyKindID.Pink);
				hub.GrantCandy(candyKindID, ItemAddReason.Scp2536);
			}
		}

		private const float PinkCandyChances = 5f;
	}
}
