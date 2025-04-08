using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items;
using PlayerRoles;
using RadialMenus;
using UnityEngine;
using UnityEngine.UI;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.GUI
{
	public class RadialInventory : RadialMenuBase, IInventoryGuiDisplayType
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			UserSetting<bool>.SetDefaultValue<MiscControlsSetting>(MiscControlsSetting.RightClickToDrop, true);
		}

		public override int Slots
		{
			get
			{
				return this._slots.Length;
			}
		}

		private static bool AllowDropping
		{
			get
			{
				return RadialInventory.DragWatch.Elapsed.TotalMilliseconds > 90.0;
			}
		}

		public void InventoryToggled(bool newState)
		{
			this._originalDragPosition = Vector3.zero;
			if (newState)
			{
				this._descriptionGroup.alpha = 0f;
				return;
			}
			this._draggedId = -1;
			this._dragCursorIcon.enabled = false;
			this._cursorDropIcon.enabled = false;
		}

		public InventoryGuiAction DisplayAndSelectItems(Inventory targetInventory, out ushort itemSerial)
		{
			bool flag = targetInventory == null;
			this.RingImage.color = this._circleColor.Color;
			try
			{
				this.RefreshItemColors(targetInventory, flag);
				this.RefreshDescriptions(targetInventory, flag);
			}
			catch (Exception ex)
			{
				global::UnityEngine.Debug.Log("Error occured at the system inventory: " + ((targetInventory == null) ? "null" : targetInventory.isLocalPlayer.ToString()));
				global::UnityEngine.Debug.LogException(ex);
			}
			itemSerial = this._highlightedSerial;
			if (Input.GetKeyDown(KeyCode.Mouse0))
			{
				this._draggedId = base.HighlightedSlot;
				ItemBase itemBase;
				if (this._draggedId >= 0 && !flag && targetInventory.UserInventory.Items.TryGetValue(this.OrganizedContent[this._draggedId], out itemBase))
				{
					this._originalDragPosition = Input.mousePosition;
					this._dragCursorIcon.texture = itemBase.Icon;
					this._dragCursorIcon.transform.position = Input.mousePosition;
					this._cursorDropIcon.color = this.RingImage.color;
				}
				else
				{
					this._originalDragPosition = Vector2.zero;
				}
			}
			if (Input.GetKeyUp(KeyCode.Mouse0))
			{
				this._dragCursorIcon.enabled = false;
				this._cursorDropIcon.enabled = false;
				this._originalDragPosition = Vector3.zero;
				if (this._draggedId >= 0 && this.OrganizedContent[this._draggedId] != 0 && base.HighlightedSlot < 0 && RadialInventory.AllowDropping)
				{
					itemSerial = this.OrganizedContent[this._draggedId];
					return InventoryGuiAction.Drop;
				}
				if (base.HighlightedSlot == this._draggedId || Mathf.Min(base.HighlightedSlot, this._draggedId) < 0 || this.OrganizedContent[this._draggedId] == 0)
				{
					if (this._organizedAmmo.Any((KeyValuePair<ItemType, AmmoElement> x) => x.Value.IsHovering()))
					{
						return InventoryGuiAction.None;
					}
					InventoryGuiController.InventoryVisible = false;
					return InventoryGuiAction.Select;
				}
				else
				{
					ushort num = this.OrganizedContent[base.HighlightedSlot];
					this.OrganizedContent[base.HighlightedSlot] = this.OrganizedContent[this._draggedId];
					this.OrganizedContent[this._draggedId] = num;
				}
			}
			if (this._dragCursorIcon.enabled)
			{
				this._dragCursorIcon.transform.position = Input.mousePosition;
				float num2;
				this._cursorDropIcon.enabled = !base.InRingRange(out num2) && RadialInventory.AllowDropping;
			}
			else if (this._draggedId >= 0 && this._originalDragPosition != Vector2.zero && Vector2.Distance(this._originalDragPosition, Input.mousePosition) > 5f && this.OrganizedContent[this._draggedId] > 0)
			{
				RadialInventory.DragWatch.Restart();
				this._dragCursorIcon.enabled = true;
				this._cursorDropIcon.enabled = false;
			}
			if (RadialInventory.RightClickToDrop.Value && Input.GetKeyDown(KeyCode.Mouse1))
			{
				return InventoryGuiAction.Drop;
			}
			return InventoryGuiAction.None;
		}

		private void RefreshDescriptions(Inventory inv, bool invNull)
		{
			if (invNull)
			{
				return;
			}
			ItemBase itemBase;
			if (this._visibleDescriptionSerial == this._highlightedSerial && inv.UserInventory.Items.TryGetValue(this._highlightedSerial, out itemBase))
			{
				foreach (RadialDescriptionBase radialDescriptionBase in this._descriptionTypes)
				{
					bool flag = radialDescriptionBase.DescriptionType == itemBase.DescriptionType;
					radialDescriptionBase.gameObject.SetActive(flag);
					if (flag)
					{
						radialDescriptionBase.UpdateInfo(itemBase, this._circleColor.Color);
					}
				}
				if (this._descriptionGroup.alpha < 1f)
				{
					this._descriptionGroup.alpha += Time.deltaTime * 15f;
				}
				return;
			}
			if (this._descriptionGroup.alpha > 0f)
			{
				this._descriptionGroup.alpha -= Time.deltaTime * 15f;
				return;
			}
			this._visibleDescriptionSerial = this._highlightedSerial;
		}

		public void ItemsModified(Inventory targetInventory)
		{
			for (int i = 0; i < this.OrganizedContent.Length; i++)
			{
				if (this.OrganizedContent[i] > 0 && !targetInventory.UserInventory.Items.ContainsKey(this.OrganizedContent[i]))
				{
					this.OrganizedContent[i] = 0;
				}
			}
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in targetInventory.UserInventory.Items)
			{
				if (!this.OrganizedContent.Contains(keyValuePair.Key))
				{
					for (int j = 0; j < this.OrganizedContent.Length; j++)
					{
						if (this.OrganizedContent[j] == 0)
						{
							this.OrganizedContent[j] = keyValuePair.Key;
							break;
						}
					}
				}
			}
		}

		public void AmmoModified(ReferenceHub hub)
		{
			PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
			bool flag = false;
			if (this._prevRole != currentRole.RoleTypeId)
			{
				flag = true;
				this._prevRole = currentRole.RoleTypeId;
			}
			foreach (KeyValuePair<ItemType, AmmoElement> keyValuePair in this._organizedAmmo)
			{
				if (flag)
				{
					global::UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
				}
				else
				{
					keyValuePair.Value.gameObject.SetActive(false);
				}
			}
			if (flag)
			{
				this._organizedAmmo.Clear();
			}
			Color roleColor = currentRole.RoleColor;
			foreach (KeyValuePair<ItemType, ushort> keyValuePair2 in hub.inventory.UserInventory.ReserveAmmo)
			{
				AmmoElement ammoElement;
				if (!this._organizedAmmo.TryGetValue(keyValuePair2.Key, out ammoElement))
				{
					ammoElement = global::UnityEngine.Object.Instantiate<AmmoElement>(this._ammoElementTemplate);
					ammoElement.transform.SetParent(this._ammoElementTemplate.transform.parent);
					ammoElement.transform.localScale = Vector3.one;
					ammoElement.Setup(keyValuePair2.Key, roleColor);
					this._organizedAmmo[keyValuePair2.Key] = ammoElement;
				}
				ammoElement.UpdateAmount((int)keyValuePair2.Value);
			}
		}

		private void RefreshItemColors(Inventory inv, bool invNull)
		{
			this._highlightedSerial = 0;
			if (this._slots == null)
			{
				return;
			}
			for (int i = 0; i < this._slots.Length; i++)
			{
				bool flag = (this._dragCursorIcon.enabled ? this._draggedId : base.HighlightedSlot) == i;
				ItemBase itemBase;
				Color color2;
				if (this.OrganizedContent[i] > 0 && !invNull && inv.UserInventory.Items.TryGetValue(this.OrganizedContent[i], out itemBase))
				{
					bool flag2 = this.OrganizedContent[i] == inv.CurItem.SerialNumber;
					IWearableItem wearableItem = itemBase as IWearableItem;
					Color color = ((wearableItem != null && wearableItem.IsWorn) ? this._wornColor.Color : (itemBase.AllowEquip ? Color.clear : this._blockedColor.Color));
					color2 = ((flag2 || flag) ? Color.Lerp(this._heldColor.Color, this._highlightColor.Color, flag ? (flag2 ? 0.5f : 1f) : 0f) : color);
					this._slots[i].UpdateVisuals(itemBase.ItemTypeId);
					if (flag)
					{
						this._highlightedSerial = this.OrganizedContent[i];
					}
				}
				else
				{
					if (invNull && flag && this.OrganizedContent[i] > 0)
					{
						color2 = this._highlightColor.Color;
						this._highlightedSerial = this.OrganizedContent[i];
					}
					else
					{
						color2 = Color.clear;
					}
					if (!invNull)
					{
						this._slots[i].UpdateVisuals(ItemType.None);
					}
				}
				Image highlightSafe = base.GetHighlightSafe(i);
				highlightSafe.color = Color.Lerp(highlightSafe.color, color2, 15f * Time.deltaTime);
			}
		}

		private static readonly CachedUserSetting<bool> RightClickToDrop = new CachedUserSetting<bool>(MiscControlsSetting.RightClickToDrop);

		[SerializeField]
		private RadialInventory.ItemSlot[] _slots;

		[SerializeField]
		private RadialDescriptionBase[] _descriptionTypes;

		[SerializeField]
		private CanvasGroup _descriptionGroup;

		[SerializeField]
		private RawImage _dragCursorIcon;

		[SerializeField]
		private Image _cursorDropIcon;

		[SerializeField]
		private AmmoElement _ammoElementTemplate;

		[SerializeField]
		private RoleAccentColor _circleColor;

		[SerializeField]
		private RoleAccentColor _highlightColor;

		[SerializeField]
		private RoleAccentColor _heldColor;

		[SerializeField]
		private RoleAccentColor _blockedColor;

		[SerializeField]
		private RoleAccentColor _wornColor;

		public readonly ushort[] OrganizedContent = new ushort[8];

		private static readonly Stopwatch DragWatch = new Stopwatch();

		private readonly Dictionary<ItemType, AmmoElement> _organizedAmmo = new Dictionary<ItemType, AmmoElement>();

		private int _draggedId;

		private ushort _highlightedSerial;

		private ushort _visibleDescriptionSerial;

		private const byte DescriptionFadeSpeed = 15;

		private const byte TransitionSpeed = 15;

		private Vector2 _originalDragPosition;

		private RoleTypeId _prevRole;

		[Serializable]
		public struct ItemSlot
		{
			public void UpdateVisuals(ItemType type)
			{
				ItemBase itemBase;
				if (type == ItemType.None || !InventoryItemLoader.AvailableItems.TryGetValue(type, out itemBase))
				{
					this._iconSlot.enabled = false;
					return;
				}
				this._iconSlot.enabled = true;
				this._iconSlot.texture = itemBase.Icon;
			}

			[SerializeField]
			private RawImage _iconSlot;
		}
	}
}
