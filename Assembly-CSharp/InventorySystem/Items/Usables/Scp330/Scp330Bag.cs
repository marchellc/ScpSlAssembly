using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330
{
	public class Scp330Bag : UsableItem, ICustomSearchCompletorItem, IAcquisitionConfirmationTrigger, IUniqueItem
	{
		public override bool CanStartUsing
		{
			get
			{
				return false;
			}
		}

		public bool AcquisitionAlreadyReceived { get; set; }

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Scp330Bag;
			}
		}

		public bool IsCandySelected
		{
			get
			{
				return this.SelectedCandyId >= 0 && this.SelectedCandyId < this.Candies.Count;
			}
		}

		public SearchCompletor GetCustomSearchCompletor(ReferenceHub hub, ItemPickupBase ipb, ItemBase ib, double disSqrt)
		{
			return new Scp330SearchCompletor(hub, ipb, ib, disSqrt);
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			base.OnAdded(pickup);
			if (!NetworkServer.active)
			{
				return;
			}
			Scp330Bag scp330Bag;
			if (!Scp330Bag.ServerProcessPickup(base.Owner, pickup as Scp330Pickup, out scp330Bag))
			{
				return;
			}
			if (scp330Bag == null || scp330Bag == this)
			{
				return;
			}
			base.ServerRemoveSelf();
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			if (NetworkServer.active)
			{
				Scp330Pickup scp330Pickup = pickup as Scp330Pickup;
				if (scp330Pickup != null && scp330Pickup != null)
				{
					scp330Pickup.StoredCandies = this.Candies;
				}
			}
		}

		public override void OnEquipped()
		{
			this.SelectedCandyId = -1;
		}

		public override void OnHolstered()
		{
			this.IsUsing = false;
		}

		public void ServerConfirmAcqusition()
		{
			this.ServerRefreshBag();
		}

		public override void ServerOnUsingCompleted()
		{
			if (!this.IsCandySelected)
			{
				return;
			}
			ICandy candy;
			if (!Scp330Candies.CandiesById.TryGetValue(this.Candies[this.SelectedCandyId], out candy))
			{
				return;
			}
			this.IsUsing = false;
			candy.ServerApplyEffects(base.Owner);
			this.Candies.RemoveAt(this.SelectedCandyId);
			base.OwnerInventory.ServerSelectItem(0);
			this.ServerRefreshBag();
		}

		public void DropCandy(int index)
		{
			this.SendClientMessage(index, true);
		}

		public void SelectCandy(int index)
		{
			this.SelectedCandyId = index;
			this.SendClientMessage(index, false);
		}

		public bool TryAddSpecific(CandyKindID kind)
		{
			if (this.Candies.Count >= 6)
			{
				return false;
			}
			this.Candies.Add(kind);
			return true;
		}

		public CandyKindID TryRemove(int index)
		{
			if (index < 0 || index > this.Candies.Count)
			{
				return CandyKindID.None;
			}
			CandyKindID candyKindID = this.Candies[index];
			this.Candies.RemoveAt(index);
			this.ServerRefreshBag();
			return candyKindID;
		}

		public bool CompareIdentical(ItemBase ib)
		{
			Scp330Bag scp330Bag = ib as Scp330Bag;
			if (scp330Bag == null)
			{
				return false;
			}
			if (this.Candies.Count != scp330Bag.Candies.Count)
			{
				return false;
			}
			for (int i = 0; i < this.Candies.Count; i++)
			{
				if (this.Candies[i] != scp330Bag.Candies[i])
				{
					return false;
				}
			}
			return true;
		}

		public static bool ServerProcessPickup(ReferenceHub ply, Scp330Pickup pickup, out Scp330Bag bag)
		{
			if (!Scp330Bag.TryGetBag(ply, out bag))
			{
				int num = (int)((pickup == null) ? 0 : pickup.Info.Serial);
				return ply.inventory.ServerAddItem(ItemType.SCP330, ItemAddReason.Scp914Upgrade, (ushort)num, pickup) != null;
			}
			bool flag = false;
			if (pickup == null)
			{
				flag = bag.TryAddSpecific(Scp330Candies.GetRandom(CandyKindID.None));
			}
			else
			{
				while (pickup.StoredCandies.Count > 0 && bag.TryAddSpecific(pickup.StoredCandies[0]))
				{
					flag = true;
					pickup.StoredCandies.RemoveAt(0);
				}
			}
			bag.ServerRefreshBag();
			return flag;
		}

		public static bool TryAddCandy(ReferenceHub hub, CandyKindID kind)
		{
			Scp330Bag scp330Bag;
			if (!Scp330Bag.TryGetBag(hub, out scp330Bag))
			{
				scp330Bag = (Scp330Bag)hub.inventory.ServerAddItem(ItemType.SCP330, ItemAddReason.Scp914Upgrade, 0, null);
				if (scp330Bag == null)
				{
					return false;
				}
				scp330Bag.Candies.Clear();
			}
			bool flag = scp330Bag.TryAddSpecific(kind);
			scp330Bag.ServerRefreshBag();
			return flag;
		}

		public static bool CanAddCandy(ReferenceHub hub)
		{
			Scp330Bag scp330Bag;
			if (!Scp330Bag.TryGetBag(hub, out scp330Bag))
			{
				return hub.inventory.UserInventory.Items.Count < 8;
			}
			return scp330Bag.Candies.Count < 6;
		}

		public static bool TryGetBag(ReferenceHub hub, out Scp330Bag bag)
		{
			bag = null;
			bool flag = false;
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in hub.inventory.UserInventory.Items)
			{
				Scp330Bag scp330Bag = keyValuePair.Value as Scp330Bag;
				if (scp330Bag != null)
				{
					bag = scp330Bag;
					flag = true;
					if (scp330Bag.Candies.Count > 0)
					{
						return true;
					}
				}
			}
			return flag;
		}

		public static void AddSimpleRegeneration(ReferenceHub hub, float rate, float duration)
		{
			AnimationCurve animationCurve = AnimationCurve.Constant(0f, duration, rate);
			UsableItemsController.GetHandler(hub).ActiveRegenerations.Add(new RegenerationProcess(animationCurve, 1f, 1f));
		}

		private void SendClientMessage(int candyIdex, bool drop)
		{
			NetworkClient.Send<SelectScp330Message>(new SelectScp330Message
			{
				Serial = base.ItemSerial,
				CandyID = (int)((byte)candyIdex),
				Drop = drop
			}, 0);
		}

		public void ServerRefreshBag()
		{
			if (this.Candies.Count > 0)
			{
				base.OwnerInventory.connectionToClient.Send<SyncScp330Message>(new SyncScp330Message
				{
					Serial = base.ItemSerial,
					Candies = this.Candies
				}, 0);
				return;
			}
			base.ServerRemoveSelf();
		}

		public int SelectedCandyId;

		public List<CandyKindID> Candies = new List<CandyKindID>();

		public const int MaxCandies = 6;
	}
}
