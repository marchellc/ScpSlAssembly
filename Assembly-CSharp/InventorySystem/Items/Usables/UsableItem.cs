using System;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables
{
	public abstract class UsableItem : ItemBase, IItemAlertDrawer, IItemDrawer, IItemDescription, IItemNametag, ISoundEmittingItem
	{
		public virtual bool CanStartUsing
		{
			get
			{
				return !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			}
		}

		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public virtual string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public virtual string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return !this.IsUsing;
			}
		}

		public virtual AlertContent Alert
		{
			get
			{
				if (this.RemainingCooldown <= 0f)
				{
					return default(AlertContent);
				}
				if (base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
				{
					return default(AlertContent);
				}
				TimeSpan timeSpan = TimeSpan.FromSeconds((double)this.RemainingCooldown);
				return new AlertContent(string.Format(UsableItem._cooldownFormat, timeSpan.ToString("mm\\:ss"), this.Name), 1f, AlertContent.ColorMode.Accented);
			}
		}

		public abstract void ServerOnUsingCompleted();

		public virtual void OnUsingStarted()
		{
			this.IsUsing = true;
			if (this.IsLocalPlayer)
			{
				UsableItemViewmodel usableItemViewmodel = this.ViewModel as UsableItemViewmodel;
				if (usableItemViewmodel != null)
				{
					usableItemViewmodel.OnUsingStarted();
				}
			}
		}

		public virtual void OnUsingCancelled()
		{
			this.IsUsing = false;
			if (this.IsLocalPlayer)
			{
				UsableItemViewmodel usableItemViewmodel = this.ViewModel as UsableItemViewmodel;
				if (usableItemViewmodel != null)
				{
					usableItemViewmodel.OnUsingCancelled();
				}
			}
		}

		protected void ServerRemoveSelf()
		{
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}

		protected void ServerSetPersonalCooldown(float timeSeconds)
		{
			UsableItemsController.GetHandler(base.Owner).PersonalCooldowns[this.ItemTypeId] = Time.timeSinceLevelLoad + timeSeconds;
		}

		protected void ServerSetGlobalItemCooldown(float timeSeconds)
		{
			UsableItemsController.GlobalItemCooldowns[base.ItemSerial] = Time.timeSinceLevelLoad + timeSeconds;
		}

		protected void ServerAddRegeneration(AnimationCurve regenCurve, float speedMultiplier = 1f, float hpMultiplier = 1f)
		{
			UsableItemsController.GetHandler(base.Owner).ActiveRegenerations.Add(new RegenerationProcess(regenCurve, speedMultiplier, hpMultiplier));
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			if (this.IsLocalPlayer)
			{
				UsableItem._useKey = NewInput.GetKey(ActionName.Shoot, KeyCode.None);
				UsableItem._cancelKey = NewInput.GetKey(ActionName.Zoom, KeyCode.None);
				UsableItem._cooldownFormat = string.Format(TranslationReader.Get("Facility", 33, "NO_TRANSLATION"), "${0}$", "${1}$");
			}
		}

		public override void OnEquipped()
		{
			if (NetworkServer.active)
			{
				float cooldown = UsableItemsController.GetCooldown(base.ItemSerial, this, UsableItemsController.GetHandler(base.Owner));
				base.OwnerInventory.connectionToClient.Send<ItemCooldownMessage>(new ItemCooldownMessage(base.ItemSerial, cooldown), 0);
			}
		}

		public override void EquipUpdate()
		{
			if (this.RemainingCooldown > 0f)
			{
				this.RemainingCooldown -= Time.deltaTime;
			}
			if (!this.IsLocalPlayer || !InventoryGuiController.ItemsSafeForInteraction || Cursor.visible)
			{
				return;
			}
			if (Input.GetKeyDown(UsableItem._useKey) && this.CanStartUsing)
			{
				NetworkClient.Send<StatusMessage>(new StatusMessage(StatusMessage.StatusType.Start, base.ItemSerial), 0);
			}
			if (Input.GetKeyDown(UsableItem._cancelKey))
			{
				NetworkClient.Send<StatusMessage>(new StatusMessage(StatusMessage.StatusType.Cancel, base.ItemSerial), 0);
			}
		}

		public virtual bool ServerValidateCancelRequest(PlayerHandler handler)
		{
			return !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
		}

		public virtual bool ServerValidateStartRequest(PlayerHandler handler)
		{
			return !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
		}

		public bool ServerTryGetSoundEmissionRange(out float range)
		{
			range = 15f;
			return this.IsUsing;
		}

		[NonSerialized]
		public float RemainingCooldown;

		[NonSerialized]
		public bool IsUsing;

		public float UseTime;

		public float MaxCancellableTime;

		[SerializeField]
		private float _weight = 1f;

		private static KeyCode _useKey;

		private static KeyCode _cancelKey;

		private static string _cooldownFormat;

		public const float AudibleSfxRange = 15f;

		public AudioClip UsingSfxClip;
	}
}
