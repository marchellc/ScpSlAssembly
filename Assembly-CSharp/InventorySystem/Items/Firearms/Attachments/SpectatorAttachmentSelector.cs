using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class SpectatorAttachmentSelector : AttachmentSelectorBase
	{
		protected override bool UseLookatMode { get; set; }

		protected override void SelectAttachmentId(byte attachmentId)
		{
			for (int i = 0; i < base.SelectedFirearm.Attachments.Length; i++)
			{
				if (base.SelectedFirearm.Attachments[i].Slot == this.SelectedSlot && base.SelectedFirearm.Attachments[i].IsEnabled)
				{
					base.SelectedFirearm.Attachments[i].IsEnabled = false;
					break;
				}
			}
			base.SelectedFirearm.Attachments[(int)attachmentId].IsEnabled = true;
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
				Vector2 vector = (x as PointerEventData).position - t.position;
				Vector2 vector2 = t.sizeDelta / 2f;
				Vector2 vector3 = vector / vector2;
				Action<Vector2> action2 = action;
				if (action2 == null)
				{
					return;
				}
				action2(vector3);
			});
			eventTrigger.triggers.Add(entry);
		}

		public void SelectFirearm(Firearm firearm)
		{
			uint savedPreferenceCode = AttachmentPreferences.GetSavedPreferenceCode(firearm.ItemTypeId);
			this._selectedFirearmId = new ItemType?(firearm.ItemTypeId);
			this._selectedCode = firearm.ValidateAttachmentsCode(savedPreferenceCode);
		}

		private void ResendPreference()
		{
			AttachmentPreferences.SavePreferenceCode(this._selectedFirearmId.Value, this._selectedCode);
			NetworkClient.Send<AttachmentsSetupPreference>(new AttachmentsSetupPreference
			{
				Weapon = this._selectedFirearmId.Value,
				AttachmentsCode = this._selectedCode
			}, 0);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnLocalClientStarted += SpectatorAttachmentSelector.SendPreferences;
			PlayerRoleManager.OnRoleChanged += SpectatorAttachmentSelector.OnRoleChanged;
		}

		private void Start()
		{
			bool flag = true;
			foreach (KeyValuePair<ItemType, ItemBase> keyValuePair in InventoryItemLoader.AvailableItems)
			{
				Firearm firearm = keyValuePair.Value as Firearm;
				if (firearm != null && firearm.Attachments.Length > 1)
				{
					SpectatorSelectorFirearmButton spectatorSelectorFirearmButton = (flag ? this._firearmButton : global::UnityEngine.Object.Instantiate<SpectatorSelectorFirearmButton>(this._firearmButton, this._firearmButton.transform.parent));
					flag = false;
					spectatorSelectorFirearmButton.Setup(this, firearm);
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
			if (this._selectedFirearmId == null)
			{
				return;
			}
			base.SelectedFirearm = AttachmentPreview.Get(this._selectedFirearmId.Value, this._selectedCode, false);
			base.LerpRects(Time.deltaTime * this._rescaleSpeed);
			MonoBehaviour[] array = this.SlotsPool;
			for (int i = 0; i < array.Length; i++)
			{
				((SpectatorSelectorCollider)array[i]).UpdateColors(this.SelectedSlot);
			}
			array = this.SelectableAttachmentsPool;
			for (int i = 0; i < array.Length; i++)
			{
				((SpectatorSelectorCollider)array[i]).UpdateColors(this.SelectedSlot);
			}
			base.RefreshState(base.SelectedFirearm, null);
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
			foreach (ItemBase itemBase in InventoryItemLoader.AvailableItems.Values)
			{
				Firearm firearm = itemBase as Firearm;
				if (firearm != null)
				{
					NetworkClient.Send<AttachmentsSetupPreference>(new AttachmentsSetupPreference
					{
						Weapon = itemBase.ItemTypeId,
						AttachmentsCode = firearm.GetSavedPreferenceCode()
					}, 0);
				}
			}
		}

		[SerializeField]
		private SpectatorSelectorFirearmButton _firearmButton;

		[SerializeField]
		private GameObject _summaryButton;

		[SerializeField]
		private float _rescaleSpeed = 10f;

		private ItemType? _selectedFirearmId;

		private uint _selectedCode;
	}
}
