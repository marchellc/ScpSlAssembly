using System;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

namespace Christmas.Scp2536.Gifts
{
	public class Scp1576 : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Exclusive;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.SCP1576, 100f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			return !Scp1576._hasBeenGranted && base.CanBeGranted(hub) && global::UnityEngine.Random.value <= 0.05f;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
			Scp1576._hasBeenGranted = true;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				Scp1576._hasBeenGranted = false;
			};
		}

		private static bool _hasBeenGranted;
	}
}
