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

namespace InventorySystem.GUI;

public class RadialInventory : RadialMenuBase, IInventoryGuiDisplayType
{
	[Serializable]
	public struct ItemSlot
	{
		[SerializeField]
		private RawImage _iconSlot;

		public void UpdateVisuals(ItemBase item)
		{
			if (item == null)
			{
				this._iconSlot.enabled = false;
				return;
			}
			this._iconSlot.enabled = true;
			this._iconSlot.texture = item.Icon;
		}
	}

	private static readonly CachedUserSetting<bool> RightClickToDrop = new CachedUserSetting<bool>(MiscControlsSetting.RightClickToDrop);

	[SerializeField]
	private ItemSlot[] _slots;

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

	public override int Slots => this._slots.Length;

	private static bool AllowDropping => RadialInventory.DragWatch.Elapsed.TotalMilliseconds > 90.0;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UserSetting<bool>.SetDefaultValue(MiscControlsSetting.RightClickToDrop, defaultValue: true);
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
		base.RingImage.color = this._circleColor.Color;
		try
		{
			this.RefreshItemColors(targetInventory, flag);
			this.RefreshDescriptions(targetInventory, flag);
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.Log("Error occured at the system inventory: " + ((targetInventory == null) ? "null" : targetInventory.isLocalPlayer.ToString()));
			UnityEngine.Debug.LogException(exception);
		}
		itemSerial = this._highlightedSerial;
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			this._draggedId = base.HighlightedSlot;
			if (this._draggedId >= 0 && !flag && targetInventory.UserInventory.Items.TryGetValue(this.OrganizedContent[this._draggedId], out var value))
			{
				this._originalDragPosition = Input.mousePosition;
				this._dragCursorIcon.texture = value.Icon;
				this._dragCursorIcon.transform.position = Input.mousePosition;
				this._cursorDropIcon.color = base.RingImage.color;
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
			ushort num = this.OrganizedContent[base.HighlightedSlot];
			this.OrganizedContent[base.HighlightedSlot] = this.OrganizedContent[this._draggedId];
			this.OrganizedContent[this._draggedId] = num;
		}
		if (this._dragCursorIcon.enabled)
		{
			this._dragCursorIcon.transform.position = Input.mousePosition;
			this._cursorDropIcon.enabled = !base.InRingRange(out var _) && RadialInventory.AllowDropping;
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
		if (this._visibleDescriptionSerial != this._highlightedSerial || !inv.UserInventory.Items.TryGetValue(this._highlightedSerial, out var value))
		{
			if (this._descriptionGroup.alpha > 0f)
			{
				this._descriptionGroup.alpha -= Time.deltaTime * 15f;
			}
			else
			{
				this._visibleDescriptionSerial = this._highlightedSerial;
			}
			return;
		}
		RadialDescriptionBase[] descriptionTypes = this._descriptionTypes;
		foreach (RadialDescriptionBase radialDescriptionBase in descriptionTypes)
		{
			bool flag = radialDescriptionBase.DescriptionType == value.DescriptionType;
			radialDescriptionBase.gameObject.SetActive(flag);
			if (flag)
			{
				radialDescriptionBase.UpdateInfo(value, this._circleColor.Color);
			}
		}
		if (this._descriptionGroup.alpha < 1f)
		{
			this._descriptionGroup.alpha += Time.deltaTime * 15f;
		}
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
		foreach (KeyValuePair<ushort, ItemBase> item in targetInventory.UserInventory.Items)
		{
			if (this.OrganizedContent.Contains(item.Key))
			{
				continue;
			}
			for (int j = 0; j < this.OrganizedContent.Length; j++)
			{
				if (this.OrganizedContent[j] == 0)
				{
					this.OrganizedContent[j] = item.Key;
					break;
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
		foreach (KeyValuePair<ItemType, AmmoElement> item in this._organizedAmmo)
		{
			if (flag)
			{
				UnityEngine.Object.Destroy(item.Value.gameObject);
			}
			else
			{
				item.Value.gameObject.SetActive(value: false);
			}
		}
		if (flag)
		{
			this._organizedAmmo.Clear();
		}
		Color roleColor = currentRole.RoleColor;
		foreach (KeyValuePair<ItemType, ushort> item2 in hub.inventory.UserInventory.ReserveAmmo)
		{
			if (!this._organizedAmmo.TryGetValue(item2.Key, out var value))
			{
				value = UnityEngine.Object.Instantiate(this._ammoElementTemplate);
				value.transform.SetParent(this._ammoElementTemplate.transform.parent);
				value.transform.localScale = Vector3.one;
				value.Setup(item2.Key, roleColor);
				this._organizedAmmo[item2.Key] = value;
			}
			value.UpdateAmount(item2.Value);
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
			Color b;
			if (this.OrganizedContent[i] > 0 && !invNull && inv.UserInventory.Items.TryGetValue(this.OrganizedContent[i], out var value))
			{
				bool flag2 = this.OrganizedContent[i] == inv.CurItem.SerialNumber;
				Color color = ((value is IWearableItem { IsWorn: not false }) ? this._wornColor.Color : (value.AllowEquip ? Color.clear : this._blockedColor.Color));
				b = ((flag2 || flag) ? Color.Lerp(this._heldColor.Color, this._highlightColor.Color, (!flag) ? 0f : (flag2 ? 0.5f : 1f)) : color);
				this._slots[i].UpdateVisuals(value);
				if (flag)
				{
					this._highlightedSerial = this.OrganizedContent[i];
				}
			}
			else
			{
				if (invNull && flag && this.OrganizedContent[i] > 0)
				{
					b = this._highlightColor.Color;
					this._highlightedSerial = this.OrganizedContent[i];
				}
				else
				{
					b = Color.clear;
				}
				if (!invNull)
				{
					this._slots[i].UpdateVisuals(null);
				}
			}
			Image highlightSafe = base.GetHighlightSafe(i);
			highlightSafe.color = Color.Lerp(highlightSafe.color, b, 15f * Time.deltaTime);
		}
	}
}
