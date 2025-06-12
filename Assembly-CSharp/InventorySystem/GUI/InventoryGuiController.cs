using System.Diagnostics;
using InventorySystem.Disarming;
using InventorySystem.Items;
using PlayerRoles;
using ToggleableMenus;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.GUI;

public class InventoryGuiController : ToggleableMenuBase, IHoldableMenu
{
	public static InventoryGuiController Singleton;

	public static readonly CachedUserSetting<bool> ToggleInventory = new CachedUserSetting<bool>(MiscControlsSetting.InventoryToggle);

	private static readonly Stopwatch CooldownStopwatch = new Stopwatch();

	private static readonly byte InventoryFadeSpeed = 10;

	[SerializeField]
	private CanvasGroup _toggleablePart;

	[SerializeField]
	private RadialInventory _displaySettings;

	private bool _prevVisible;

	public static bool InventoryVisible
	{
		get
		{
			if (InventoryGuiController.Singleton != null)
			{
				return InventoryGuiController.Singleton.IsEnabled;
			}
			return false;
		}
		set
		{
			if (!(InventoryGuiController.Singleton == null))
			{
				InventoryGuiController.Singleton.IsEnabled = value;
			}
		}
	}

	public static bool ItemsSafeForInteraction
	{
		get
		{
			if (InventoryGuiController.Singleton._toggleablePart.alpha > 0f || Cursor.visible)
			{
				if (!InventoryGuiController.CooldownStopwatch.IsRunning)
				{
					InventoryGuiController.CooldownStopwatch.Restart();
				}
				return false;
			}
			if (InventoryGuiController.CooldownStopwatch.IsRunning)
			{
				if (InventoryGuiController.CooldownStopwatch.Elapsed.TotalSeconds < 0.10000000149011612)
				{
					return false;
				}
				InventoryGuiController.CooldownStopwatch.Stop();
			}
			return true;
		}
	}

	public override bool CanToggle
	{
		get
		{
			if (!this.IsEnabled)
			{
				return InventoryGuiController.CanInventoryBeDisplayed();
			}
			return true;
		}
	}

	public static IInventoryGuiDisplayType DisplayController => InventoryGuiController.Singleton._displaySettings;

	private static Inventory UserInventory => ReferenceHub.LocalHub.inventory;

	public bool IsHoldable => !InventoryGuiController.ToggleInventory.Value;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UserSetting<bool>.SetDefaultValue(MiscControlsSetting.InventoryToggle, defaultValue: true);
	}

	private void ItemsModified(ReferenceHub hub)
	{
		if (hub.isLocalPlayer)
		{
			InventoryGuiController.DisplayController.ItemsModified(hub.inventory);
		}
	}

	private void AmmoModified(ReferenceHub hub)
	{
		if (hub.isLocalPlayer)
		{
			InventoryGuiController.DisplayController.AmmoModified(hub);
		}
	}

	private void RoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (hub.isLocalPlayer)
		{
			InventoryGuiController.DisplayController.ItemsModified(hub.inventory);
			InventoryGuiController.DisplayController.AmmoModified(hub);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		InventoryGuiController.Singleton = this;
		this.IsEnabled = false;
		Inventory.OnItemsModified += ItemsModified;
		Inventory.OnAmmoModified += AmmoModified;
		PlayerRoleManager.OnRoleChanged += RoleChanged;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Inventory.OnItemsModified -= ItemsModified;
		Inventory.OnAmmoModified -= AmmoModified;
		PlayerRoleManager.OnRoleChanged -= RoleChanged;
	}

	private void Update()
	{
		this.RefreshAnimations(forceNoAnimations: false);
		if (InventoryGuiController.InventoryVisible)
		{
			if (!InventoryGuiController.CanInventoryBeDisplayed())
			{
				this.IsEnabled = false;
			}
			ushort itemSerial;
			switch (InventoryGuiController.DisplayController.DisplayAndSelectItems(InventoryGuiController.UserInventory, out itemSerial))
			{
			case InventoryGuiAction.Select:
				InventoryGuiController.UserInventory.ClientSelectItem(itemSerial);
				break;
			case InventoryGuiAction.Drop:
				InventoryGuiController.UserInventory.ClientDropItem(itemSerial, tryThrow: false);
				break;
			}
		}
		if (this._prevVisible != InventoryGuiController.InventoryVisible)
		{
			InventoryGuiController.DisplayController.InventoryToggled(InventoryGuiController.InventoryVisible);
			this._prevVisible = InventoryGuiController.InventoryVisible;
		}
	}

	private void RefreshAnimations(bool forceNoAnimations)
	{
		if (InventoryGuiController.InventoryVisible)
		{
			if (!this._toggleablePart.gameObject.activeSelf)
			{
				this._toggleablePart.gameObject.SetActive(value: true);
			}
			if (this._toggleablePart.alpha < 1f)
			{
				this._toggleablePart.alpha = (forceNoAnimations ? 1f : Mathf.Clamp01(this._toggleablePart.alpha + Time.deltaTime * (float)(int)InventoryGuiController.InventoryFadeSpeed));
			}
		}
		else
		{
			if (!this._toggleablePart.gameObject.activeSelf)
			{
				return;
			}
			if (forceNoAnimations)
			{
				this._toggleablePart.alpha = 0f;
				this._toggleablePart.gameObject.SetActive(value: false);
				return;
			}
			this._toggleablePart.alpha = Mathf.Clamp01(this._toggleablePart.alpha - Time.deltaTime * (float)(int)InventoryGuiController.InventoryFadeSpeed);
			if (this._toggleablePart.alpha <= 0f)
			{
				this._toggleablePart.gameObject.SetActive(value: false);
			}
		}
	}

	public static bool CanInventoryBeDisplayed()
	{
		if (!ReferenceHub.TryGetLocalHub(out var hub) || !(hub.roleManager.CurrentRole is IInventoryRole))
		{
			return false;
		}
		if (hub.inventory.IsDisarmed())
		{
			return false;
		}
		if (InventoryGuiController.UserInventory.CurInstance != null && !InventoryGuiController.UserInventory.CurInstance.AllowHolster)
		{
			return false;
		}
		return !hub.interCoordinator.AnyBlocker((IInteractionBlocker x) => x.BlockedInteractions.HasFlagFast(BlockedInteraction.OpenInventory));
	}

	protected override void OnToggled()
	{
		this.RefreshAnimations(!InventoryGuiController.CanInventoryBeDisplayed());
	}
}
