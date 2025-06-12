using System;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using Mirror;
using NetworkManagerUtils.Dummies;
using UnityEngine;

namespace InventorySystem.Items.Usables;

public abstract class UsableItem : ItemBase, IItemAlertDrawer, IItemDrawer, IItemDescription, IItemNametag, ISoundEmittingItem, IDummyActionProvider
{
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

	public virtual bool CanStartUsing => !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);

	public override float Weight => this._weight;

	public virtual string Description => base.ItemTypeId.GetDescription();

	public virtual string Name => base.ItemTypeId.GetName();

	public override bool AllowHolster => !this.IsUsing;

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
			TimeSpan timeSpan = TimeSpan.FromSeconds(this.RemainingCooldown);
			return new AlertContent(string.Format(UsableItem._cooldownFormat, timeSpan.ToString("mm\\:ss"), this.Name));
		}
	}

	public bool DummyActionsDirty => false;

	public abstract void ServerOnUsingCompleted();

	public virtual void OnUsingStarted()
	{
		this.IsUsing = true;
		if (this.IsLocalPlayer && base.ViewModel is UsableItemViewmodel usableItemViewmodel)
		{
			usableItemViewmodel.OnUsingStarted();
		}
	}

	public virtual void OnUsingCancelled()
	{
		this.IsUsing = false;
		if (this.IsLocalPlayer && base.ViewModel is UsableItemViewmodel usableItemViewmodel)
		{
			usableItemViewmodel.OnUsingCancelled();
		}
	}

	protected void ServerRemoveSelf()
	{
		base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
	}

	protected void ServerSetPersonalCooldown(float timeSeconds)
	{
		UsableItemsController.GetHandler(base.Owner).PersonalCooldowns[base.ItemTypeId] = Time.timeSinceLevelLoad + timeSeconds;
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
			UsableItem._useKey = NewInput.GetKey(ActionName.Shoot);
			UsableItem._cancelKey = NewInput.GetKey(ActionName.Zoom);
			UsableItem._cooldownFormat = string.Format(TranslationReader.Get("Facility", 33), "${0}$", "${1}$");
		}
	}

	public override void OnEquipped()
	{
		if (NetworkServer.active)
		{
			float cooldown = UsableItemsController.GetCooldown(base.ItemSerial, this, UsableItemsController.GetHandler(base.Owner));
			base.OwnerInventory.connectionToClient.Send(new ItemCooldownMessage(base.ItemSerial, cooldown));
		}
	}

	public override void EquipUpdate()
	{
		if (this.RemainingCooldown > 0f)
		{
			this.RemainingCooldown -= Time.deltaTime;
		}
		if (this.IsLocalPlayer && InventoryGuiController.ItemsSafeForInteraction && !Cursor.visible)
		{
			if (Input.GetKeyDown(UsableItem._useKey) && this.CanStartUsing)
			{
				NetworkClient.Send(new StatusMessage(StatusMessage.StatusType.Start, base.ItemSerial));
			}
			if (Input.GetKeyDown(UsableItem._cancelKey))
			{
				NetworkClient.Send(new StatusMessage(StatusMessage.StatusType.Cancel, base.ItemSerial));
			}
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

	public virtual void PopulateDummyActions(Action<DummyAction> actionAdder)
	{
		if (base.IsEquipped)
		{
			actionAdder(GenerateAction(StatusMessage.StatusType.Start, base.ItemSerial));
			actionAdder(GenerateAction(StatusMessage.StatusType.Cancel, base.ItemSerial));
		}
		static DummyAction GenerateAction(StatusMessage.StatusType status, ushort serial)
		{
			return new DummyAction(string.Format("{0}->{1}", "UsableItem", status), delegate
			{
				UsableItemsController.ServerEmulateMessage(serial, status);
			});
		}
	}
}
