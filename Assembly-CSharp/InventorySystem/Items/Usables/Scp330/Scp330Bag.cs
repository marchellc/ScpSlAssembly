using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using Mirror;
using NetworkManagerUtils.Dummies;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330;

public class Scp330Bag : UsableItem, IAcquisitionConfirmationTrigger, IUniqueItem
{
	public int SelectedCandyId;

	public List<CandyKindID> Candies = new List<CandyKindID>();

	public const int MaxCandies = 6;

	public override bool CanStartUsing => false;

	public bool AcquisitionAlreadyReceived { get; set; }

	public override ItemDescriptionType DescriptionType => ItemDescriptionType.Scp330Bag;

	public bool IsCandySelected
	{
		get
		{
			if (this.SelectedCandyId >= 0)
			{
				return this.SelectedCandyId < this.Candies.Count;
			}
			return false;
		}
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (NetworkServer.active && Scp330Bag.ServerProcessPickup(base.Owner, pickup as Scp330Pickup, out var bag) && !(bag == null) && !(bag == this))
		{
			base.ServerRemoveSelf();
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (NetworkServer.active && pickup is Scp330Pickup scp330Pickup && scp330Pickup != null)
		{
			scp330Pickup.StoredCandies = this.Candies;
		}
	}

	public override void OnEquipped()
	{
		this.SelectedCandyId = -1;
	}

	public override void OnHolstered()
	{
		base.IsUsing = false;
	}

	public void ServerConfirmAcqusition()
	{
		this.ServerRefreshBag();
	}

	public override void ServerOnUsingCompleted()
	{
		if (this.IsCandySelected && Scp330Candies.CandiesById.TryGetValue(this.Candies[this.SelectedCandyId], out var value))
		{
			base.IsUsing = false;
			value.ServerApplyEffects(base.Owner);
			this.Candies.RemoveAt(this.SelectedCandyId);
			base.OwnerInventory.ServerSelectItem(0);
			this.ServerRefreshBag();
		}
	}

	public void DropCandy(int index)
	{
		this.SendClientMessage(index, drop: true);
	}

	public void SelectCandy(int index)
	{
		this.SelectedCandyId = index;
		this.SendClientMessage(index, drop: false);
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
		CandyKindID result = this.Candies[index];
		this.Candies.RemoveAt(index);
		this.ServerRefreshBag();
		return result;
	}

	public bool CompareIdentical(ItemBase ib)
	{
		if (!(ib is Scp330Bag scp330Bag))
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
			int num = ((!(pickup == null)) ? pickup.Info.Serial : 0);
			return ply.inventory.ServerAddItem(ItemType.SCP330, ItemAddReason.Scp914Upgrade, (ushort)num, pickup) != null;
		}
		bool result = false;
		if (pickup == null)
		{
			result = bag.TryAddSpecific(Scp330Candies.GetRandom());
		}
		else
		{
			while (pickup.StoredCandies.Count > 0 && bag.TryAddSpecific(pickup.StoredCandies[0]))
			{
				result = true;
				pickup.StoredCandies.RemoveAt(0);
			}
		}
		bag.ServerRefreshBag();
		return result;
	}

	public static bool TryAddCandy(ReferenceHub hub, CandyKindID kind)
	{
		if (!Scp330Bag.TryGetBag(hub, out var bag))
		{
			bag = (Scp330Bag)hub.inventory.ServerAddItem(ItemType.SCP330, ItemAddReason.Scp914Upgrade, 0);
			if (bag == null)
			{
				return false;
			}
			bag.Candies.Clear();
		}
		bool result = bag.TryAddSpecific(kind);
		bag.ServerRefreshBag();
		return result;
	}

	public static bool CanAddCandy(ReferenceHub hub)
	{
		if (!Scp330Bag.TryGetBag(hub, out var bag))
		{
			return hub.inventory.UserInventory.Items.Count < 8;
		}
		return bag.Candies.Count < 6;
	}

	public static bool TryGetBag(ReferenceHub hub, out Scp330Bag bag)
	{
		bag = null;
		bool result = false;
		foreach (KeyValuePair<ushort, ItemBase> item in hub.inventory.UserInventory.Items)
		{
			if (item.Value is Scp330Bag scp330Bag)
			{
				bag = scp330Bag;
				result = true;
				if (scp330Bag.Candies.Count > 0)
				{
					return true;
				}
			}
		}
		return result;
	}

	public static void AddSimpleRegeneration(ReferenceHub hub, float rate, float duration)
	{
		AnimationCurve regenCurve = AnimationCurve.Constant(0f, duration, rate);
		UsableItemsController.GetHandler(hub).ActiveRegenerations.Add(new RegenerationProcess(regenCurve, 1f, 1f));
	}

	private void SendClientMessage(int candyIdex, bool drop)
	{
		NetworkClient.Send(new SelectScp330Message
		{
			Serial = base.ItemSerial,
			CandyID = (byte)candyIdex,
			Drop = drop
		});
	}

	public void ServerRefreshBag()
	{
		if (this.Candies.Count > 0)
		{
			base.OwnerInventory.connectionToClient.Send(new SyncScp330Message
			{
				Serial = base.ItemSerial,
				Candies = this.Candies
			});
		}
		else
		{
			base.ServerRemoveSelf();
		}
	}

	public override void PopulateDummyActions(Action<DummyAction> actionAdder)
	{
		if (!base.IsEquipped)
		{
			return;
		}
		for (int i = 0; i < this.Candies.Count; i++)
		{
			int copyI = i;
			actionAdder(new DummyAction(string.Format("{0}->Eat_{1}_{2}", "Scp330Bag", i, this.Candies[i]), delegate
			{
				this.ServerSelectCandy(copyI);
			}));
		}
		for (int num = 0; num < this.Candies.Count; num++)
		{
			int copyI2 = num;
			actionAdder(new DummyAction(string.Format("{0}->Drop_{1}_{2}", "Scp330Bag", num, this.Candies[num]), delegate
			{
				this.ServerDropCandy(copyI2);
			}));
		}
	}
}
