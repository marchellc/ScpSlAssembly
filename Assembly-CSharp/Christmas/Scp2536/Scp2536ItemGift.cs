using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

namespace Christmas.Scp2536
{
	public abstract class Scp2536ItemGift : Scp2536GiftBase
	{
		protected Scp2536ItemGift()
		{
			this._chanceSum = this.Rewards.Sum((Scp2536Reward r) => r.Weight);
		}

		protected abstract Scp2536Reward[] Rewards { get; }

		protected void GrantAllRewards(ReferenceHub hub)
		{
			Scp2536Reward[] rewards = this.Rewards;
			for (int i = 0; i < rewards.Length; i++)
			{
				ItemType reward = rewards[i].Reward;
				Dictionary<ushort, ItemBase> items = hub.inventory.UserInventory.Items;
				if (items.Count >= 8)
				{
					hub.inventory.ServerDropItem(items.ElementAt(items.Count - 1).Key);
				}
				hub.inventory.ServerAddItem(reward, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
			}
		}

		protected ItemType GenerateRandomReward()
		{
			float num = global::UnityEngine.Random.Range(0f, this._chanceSum);
			ItemType itemType = ItemType.Coin;
			foreach (Scp2536Reward scp2536Reward in this.Rewards)
			{
				itemType = scp2536Reward.Reward;
				num -= scp2536Reward.Weight;
				if (num <= 0f)
				{
					return itemType;
				}
			}
			return itemType;
		}

		private readonly float _chanceSum;
	}
}
