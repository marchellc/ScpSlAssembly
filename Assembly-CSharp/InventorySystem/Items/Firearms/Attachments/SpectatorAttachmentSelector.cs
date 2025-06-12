using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InventorySystem.Items.Firearms.Attachments;

public class SpectatorAttachmentSelector : AttachmentSelectorBase
{
	[SerializeField]
	private SpectatorSelectorFirearmButton _firearmButton;

	[SerializeField]
	private GameObject _summaryButton;

	[SerializeField]
	private float _rescaleSpeed = 10f;

	private ItemType? _selectedFirearmId;

	private uint _selectedCode;

	protected override bool UseLookatMode { get; set; }

	protected override void SelectAttachmentId(byte attachmentId)
	{
		for (int i = 0; i < base.SelectedFirearm.Attachments.Length; i++)
		{
			if (base.SelectedFirearm.Attachments[i].Slot == base.SelectedSlot && base.SelectedFirearm.Attachments[i].IsEnabled)
			{
				base.SelectedFirearm.Attachments[i].IsEnabled = false;
				break;
			}
		}
		base.SelectedFirearm.Attachments[attachmentId].IsEnabled = true;
		this._selectedCode = base.SelectedFirearm.GetCurrentAttachmentsCode();
		this._selectedCode = base.SelectedFirearm.ValidateAttachmentsCode(this._selectedCode);
		AttachmentPreferences.SetPreset(base.SelectedFirearm.ItemTypeId, 0);
		this.ResendPreference();
	}

	protected override void LoadPreset(uint loadedCode)
	{
		this._selectedCode = loadedCode;
		this.ResendPreference();
	}

	public override void RegisterAction(RectTransform t, Action<Vector2> action)
	{
		EventTrigger eventTrigger = t.gameObject.AddComponent<EventTrigger>();
		EventTrigger.Entry entry = new EventTrigger.Entry
		{
			eventID = EventTriggerType.PointerDown
		};
		entry.callback.AddListener(delegate(BaseEventData x)
		{
			Vector2 vector = (x as PointerEventData).position - (Vector2)t.position;
			Vector2 vector2 = t.sizeDelta / 2f;
			Vector2 obj = vector / vector2;
			action?.Invoke(obj);
		});
		eventTrigger.triggers.Add(entry);
	}

	public void SelectFirearm(Firearm firearm)
	{
		uint savedPreferenceCode = AttachmentPreferences.GetSavedPreferenceCode(firearm.ItemTypeId);
		this._selectedFirearmId = firearm.ItemTypeId;
		this._selectedCode = firearm.ValidateAttachmentsCode(savedPreferenceCode);
	}

	private void ResendPreference()
	{
		AttachmentPreferences.SavePreferenceCode(this._selectedFirearmId.Value, this._selectedCode);
		NetworkClient.Send(new AttachmentsSetupPreference
		{
			Weapon = this._selectedFirearmId.Value,
			AttachmentsCode = this._selectedCode
		});
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Inventory.OnLocalClientStarted += SendPreferences;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private void Start()
	{
		bool flag = true;
		foreach (KeyValuePair<ItemType, ItemBase> availableItem in InventoryItemLoader.AvailableItems)
		{
			if (availableItem.Value is Firearm firearm && firearm.Attachments.Length > 1)
			{
				SpectatorSelectorFirearmButton obj = (flag ? this._firearmButton : UnityEngine.Object.Instantiate(this._firearmButton, this._firearmButton.transform.parent));
				flag = false;
				obj.Setup(this, firearm);
			}
		}
	}

	private void OnDisable()
	{
		base.RefreshState(null, null);
		this._selectedFirearmId = null;
	}

	private void Update()
	{
		if (this._selectedFirearmId.HasValue)
		{
			base.SelectedFirearm = AttachmentPreview.Get(this._selectedFirearmId.Value, this._selectedCode);
			base.LerpRects(Time.deltaTime * this._rescaleSpeed);
			MonoBehaviour[] slotsPool = base.SlotsPool;
			for (int i = 0; i < slotsPool.Length; i++)
			{
				((SpectatorSelectorCollider)slotsPool[i]).UpdateColors(base.SelectedSlot);
			}
			slotsPool = base.SelectableAttachmentsPool;
			for (int i = 0; i < slotsPool.Length; i++)
			{
				((SpectatorSelectorCollider)slotsPool[i]).UpdateColors(base.SelectedSlot);
			}
			base.RefreshState(base.SelectedFirearm, null);
		}
	}

	private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (hub.isLocalPlayer && !hub.IsAlive() && NetworkClient.active)
		{
			SpectatorAttachmentSelector.SendPreferences();
		}
	}

	private static void SendPreferences()
	{
		foreach (ItemBase value in InventoryItemLoader.AvailableItems.Values)
		{
			if (value is Firearm weapon)
			{
				NetworkClient.Send(new AttachmentsSetupPreference
				{
					Weapon = value.ItemTypeId,
					AttachmentsCode = weapon.GetSavedPreferenceCode()
				});
			}
		}
	}
}
