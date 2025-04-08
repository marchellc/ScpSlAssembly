using System;
using CustomPlayerEffects;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Armor
{
	public class BodyArmor : ItemBase, IWearableItem, IItemNametag, ICustomSearchCompletorItem, IMovementSpeedModifier, IStaminaModifier
	{
		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public bool IsWorn
		{
			get
			{
				return true;
			}
		}

		public WearableSlot Slot
		{
			get
			{
				return WearableSlot.Body;
			}
		}

		public override bool AllowEquip
		{
			get
			{
				return false;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return this.IsWorn && !IHeavyItemPenaltyImmunity.IsImmune(base.Owner);
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return this.ProcessMultiplier(this._movementSpeedMultiplier);
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return this.IsWorn && !IHeavyItemPenaltyImmunity.IsImmune(base.Owner);
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return this.ProcessMultiplier(this._staminaUseMultiplier);
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Armor;
			}
		}

		private float ProcessMultiplier(float f)
		{
			Team team = base.Owner.GetTeam();
			if (team == Team.ClassD || team == Team.Scientists)
			{
				f = (f - 1f) * this.CivilianClassDownsidesMultiplier + 1f;
			}
			return f;
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public SearchCompletor GetCustomSearchCompletor(ReferenceHub hub, ItemPickupBase ipb, ItemBase ib, double disSqrt)
		{
			return new ArmorSearchCompletor(hub, ipb, ib, disSqrt);
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			if (!NetworkServer.active || this.DontRemoveExcessOnDrop)
			{
				return;
			}
			base.OwnerInventory.RemoveEverythingExceedingLimits(null, true, true);
		}

		[NonSerialized]
		public bool DontRemoveExcessOnDrop;

		[Range(0f, 100f)]
		public int HelmetEfficacy;

		[Range(0f, 100f)]
		public int VestEfficacy;

		public float CivilianClassDownsidesMultiplier = 1f;

		public BodyArmor.ArmorAmmoLimit[] AmmoLimits;

		public BodyArmor.ArmorCategoryLimitModifier[] CategoryLimits;

		[SerializeField]
		[Range(1f, 2f)]
		private float _staminaUseMultiplier = 1f;

		[SerializeField]
		[Range(0f, 1f)]
		private float _movementSpeedMultiplier = 1f;

		[SerializeField]
		private float _weight;

		[Serializable]
		public struct ArmorAmmoLimit
		{
			public ItemType AmmoType;

			public ushort Limit;
		}

		[Serializable]
		public struct ArmorCategoryLimitModifier
		{
			public ItemCategory Category;

			public byte Limit;
		}
	}
}
