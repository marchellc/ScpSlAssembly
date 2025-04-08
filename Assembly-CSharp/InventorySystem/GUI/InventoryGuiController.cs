using System;
using System.Diagnostics;
using InventorySystem.Disarming;
using InventorySystem.Items;
using PlayerRoles;
using ToggleableMenus;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.GUI
{
	public class InventoryGuiController : ToggleableMenuBase, IHoldableMenu
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			UserSetting<bool>.SetDefaultValue<MiscControlsSetting>(MiscControlsSetting.InventoryToggle, true);
		}

		public static bool InventoryVisible
		{
			get
			{
				return InventoryGuiController.Singleton != null && InventoryGuiController.Singleton.IsEnabled;
			}
			set
			{
				if (InventoryGuiController.Singleton == null)
				{
					return;
				}
				InventoryGuiController.Singleton.IsEnabled = value;
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
				return this.IsEnabled || InventoryGuiController.CanInventoryBeDisplayed();
			}
		}

		public static IInventoryGuiDisplayType DisplayController
		{
			get
			{
				return InventoryGuiController.Singleton._displaySettings;
			}
		}

		private static Inventory UserInventory
		{
			get
			{
				return ReferenceHub.LocalHub.inventory;
			}
		}

		private void ItemsModified(ReferenceHub hub)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			InventoryGuiController.DisplayController.ItemsModified(hub.inventory);
		}

		private void AmmoModified(ReferenceHub hub)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			InventoryGuiController.DisplayController.AmmoModified(hub);
		}

		private void RoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			InventoryGuiController.DisplayController.ItemsModified(hub.inventory);
			InventoryGuiController.DisplayController.AmmoModified(hub);
		}

		protected override void Awake()
		{
			base.Awake();
			InventoryGuiController.Singleton = this;
			this.IsEnabled = false;
			Inventory.OnItemsModified += this.ItemsModified;
			Inventory.OnAmmoModified += this.AmmoModified;
			PlayerRoleManager.OnRoleChanged += this.RoleChanged;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Inventory.OnItemsModified -= this.ItemsModified;
			Inventory.OnAmmoModified -= this.AmmoModified;
			PlayerRoleManager.OnRoleChanged -= this.RoleChanged;
		}

		private void Update()
		{
			this.RefreshAnimations(false);
			if (InventoryGuiController.InventoryVisible)
			{
				if (!InventoryGuiController.CanInventoryBeDisplayed())
				{
					this.IsEnabled = false;
				}
				ushort num;
				InventoryGuiAction inventoryGuiAction = InventoryGuiController.DisplayController.DisplayAndSelectItems(InventoryGuiController.UserInventory, out num);
				if (inventoryGuiAction == InventoryGuiAction.Select)
				{
					InventoryGuiController.UserInventory.ClientSelectItem(num);
				}
				else if (inventoryGuiAction == InventoryGuiAction.Drop)
				{
					InventoryGuiController.UserInventory.ClientDropItem(num, false);
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
					this._toggleablePart.gameObject.SetActive(true);
				}
				if (this._toggleablePart.alpha < 1f)
				{
					this._toggleablePart.alpha = (forceNoAnimations ? 1f : Mathf.Clamp01(this._toggleablePart.alpha + Time.deltaTime * (float)InventoryGuiController.InventoryFadeSpeed));
					return;
				}
			}
			else if (this._toggleablePart.gameObject.activeSelf)
			{
				if (forceNoAnimations)
				{
					this._toggleablePart.alpha = 0f;
					this._toggleablePart.gameObject.SetActive(false);
					return;
				}
				this._toggleablePart.alpha = Mathf.Clamp01(this._toggleablePart.alpha - Time.deltaTime * (float)InventoryGuiController.InventoryFadeSpeed);
				if (this._toggleablePart.alpha <= 0f)
				{
					this._toggleablePart.gameObject.SetActive(false);
				}
			}
		}

		public static bool CanInventoryBeDisplayed()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub) || !(referenceHub.roleManager.CurrentRole is IInventoryRole))
			{
				return false;
			}
			if (referenceHub.inventory.IsDisarmed())
			{
				return false;
			}
			if (InventoryGuiController.UserInventory.CurInstance != null && !InventoryGuiController.UserInventory.CurInstance.AllowHolster)
			{
				return false;
			}
			return !referenceHub.interCoordinator.AnyBlocker((IInteractionBlocker x) => x.BlockedInteractions.HasFlagFast(BlockedInteraction.OpenInventory));
		}

		protected override void OnToggled()
		{
			this.RefreshAnimations(!InventoryGuiController.CanInventoryBeDisplayed());
		}

		public bool IsHoldable
		{
			get
			{
				return !InventoryGuiController.ToggleInventory.Value;
			}
		}

		public static InventoryGuiController Singleton;

		public static readonly CachedUserSetting<bool> ToggleInventory = new CachedUserSetting<bool>(MiscControlsSetting.InventoryToggle);

		private static readonly Stopwatch CooldownStopwatch = new Stopwatch();

		private static readonly byte InventoryFadeSpeed = 10;

		[SerializeField]
		private CanvasGroup _toggleablePart;

		[SerializeField]
		private RadialInventory _displaySettings;

		private bool _prevVisible;
	}
}
