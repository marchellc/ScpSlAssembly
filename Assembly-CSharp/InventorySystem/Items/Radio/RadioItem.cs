using System;
using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Scp914;
using UnityEngine;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio
{
	public class RadioItem : ItemBase, IAcquisitionConfirmationTrigger, IItemDescription, IItemNametag, IUpgradeTrigger, IUniqueItem
	{
		public bool AcquisitionAlreadyReceived { get; set; }

		public override float Weight
		{
			get
			{
				return 1.7f;
			}
		}

		public bool IsUsable
		{
			get
			{
				return this._enabled && this._battery > 0f;
			}
		}

		public string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public byte BatteryPercent
		{
			get
			{
				return (byte)Mathf.RoundToInt(this._battery * 100f);
			}
			set
			{
				this._battery = (float)value / 100f;
			}
		}

		public RadioMessages.RadioRangeLevel RangeLevel
		{
			get
			{
				return (RadioMessages.RadioRangeLevel)(this._enabled ? this._rangeId : ((byte)(-1)));
			}
		}

		public void ServerConfirmAcqusition()
		{
			this.SendStatusMessage();
		}

		public bool CompareIdentical(ItemBase other)
		{
			RadioItem radioItem = other as RadioItem;
			return radioItem != null && this.RangeLevel == radioItem.RangeLevel && this.BatteryPercent == radioItem.BatteryPercent;
		}

		public void ServerOnUpgraded(Scp914KnobSetting setting)
		{
			this.BatteryPercent = 100;
			this.SendStatusMessage();
		}

		public override void OnAdded(ItemPickupBase ipb)
		{
			if (this.IsLocalPlayer)
			{
				RadioItem._circleModeKey = NewInput.GetKey(ActionName.Shoot, KeyCode.None);
				RadioItem._toggleKey = NewInput.GetKey(ActionName.Zoom, KeyCode.None);
			}
			if (!NetworkServer.active)
			{
				return;
			}
			RadioPickup radioPickup = ipb as RadioPickup;
			if (radioPickup != null)
			{
				this._enabled = radioPickup.SavedEnabled;
				this._rangeId = radioPickup.SavedRange;
				this._battery = radioPickup.SavedBattery;
				return;
			}
			this._enabled = true;
			this._battery = 1f;
			this._rangeId = 1;
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			if (NetworkServer.active)
			{
				RadioPickup radioPickup = pickup as RadioPickup;
				if (radioPickup != null)
				{
					radioPickup.NetworkSavedEnabled = this._enabled;
					radioPickup.NetworkSavedRange = this._rangeId;
					radioPickup.SavedBattery = this._battery;
				}
			}
		}

		public override void OnEquipped()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.SendStatusMessage();
		}

		public override void EquipUpdate()
		{
			if (!this.IsLocalPlayer || !InventoryGuiController.ItemsSafeForInteraction || Cursor.visible)
			{
				return;
			}
			if (Input.GetKeyDown(RadioItem._circleModeKey) && this._enabled)
			{
				NetworkClient.Send<ClientRadioCommandMessage>(new ClientRadioCommandMessage(RadioMessages.RadioCommand.ChangeRange), 0);
			}
			if (Input.GetKeyDown(RadioItem._toggleKey))
			{
				NetworkClient.Send<ClientRadioCommandMessage>(new ClientRadioCommandMessage(this._enabled ? RadioMessages.RadioCommand.Disable : RadioMessages.RadioCommand.Enable), 0);
			}
		}

		public void ServerProcessCmd(RadioMessages.RadioCommand command)
		{
			if (base.Owner.HasBlock(BlockedInteraction.ItemUsage))
			{
				return;
			}
			switch (command)
			{
			case RadioMessages.RadioCommand.Enable:
			{
				if (this._enabled || this._battery <= 0f)
				{
					return;
				}
				PlayerTogglingRadioEventArgs playerTogglingRadioEventArgs = new PlayerTogglingRadioEventArgs(base.Owner, this, true);
				PlayerEvents.OnTogglingRadio(playerTogglingRadioEventArgs);
				if (!playerTogglingRadioEventArgs.IsAllowed)
				{
					return;
				}
				this._enabled = true;
				PlayerEvents.OnToggledRadio(new PlayerToggledRadioEventArgs(base.Owner, this, true));
				break;
			}
			case RadioMessages.RadioCommand.Disable:
			{
				if (!this._enabled)
				{
					return;
				}
				PlayerTogglingRadioEventArgs playerTogglingRadioEventArgs2 = new PlayerTogglingRadioEventArgs(base.Owner, this, false);
				PlayerEvents.OnTogglingRadio(playerTogglingRadioEventArgs2);
				if (!playerTogglingRadioEventArgs2.IsAllowed)
				{
					return;
				}
				this._enabled = false;
				PlayerEvents.OnToggledRadio(new PlayerToggledRadioEventArgs(base.Owner, this, false));
				break;
			}
			case RadioMessages.RadioCommand.ChangeRange:
			{
				byte b = this._rangeId + 1;
				if ((int)b >= this.Ranges.Length)
				{
					b = 0;
				}
				PlayerChangingRadioRangeEventArgs playerChangingRadioRangeEventArgs = new PlayerChangingRadioRangeEventArgs(base.Owner, this, (RadioMessages.RadioRangeLevel)b);
				PlayerEvents.OnChangingRadioRange(playerChangingRadioRangeEventArgs);
				if (!playerChangingRadioRangeEventArgs.IsAllowed)
				{
					return;
				}
				b = (byte)playerChangingRadioRangeEventArgs.Range;
				this._rangeId = b;
				PlayerEvents.OnChangedRadioRange(new PlayerChangedRadioRangeEventArgs(base.Owner, this, (RadioMessages.RadioRangeLevel)b));
				break;
			}
			}
			this.SendStatusMessage();
		}

		public void UserReceiveInfo(RadioStatusMessage info)
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			this._enabled = info.Range != RadioMessages.RadioRangeLevel.RadioDisabled;
			this.BatteryPercent = info.Battery;
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (!this.IsUsable)
			{
				return;
			}
			float num = (PersonalRadioPlayback.IsTransmitting(base.Owner) ? ((float)this.Ranges[(int)this._rangeId].MinuteCostWhenTalking) : this.Ranges[(int)this._rangeId].MinuteCostWhenIdle);
			float num2 = Time.deltaTime * 0.5f * (num / 60f / 100f);
			PlayerUsingRadioEventArgs playerUsingRadioEventArgs = new PlayerUsingRadioEventArgs(base.Owner, this, num2);
			PlayerEvents.OnUsingRadio(playerUsingRadioEventArgs);
			if (!playerUsingRadioEventArgs.IsAllowed)
			{
				return;
			}
			num2 = playerUsingRadioEventArgs.Drain;
			this._battery = Mathf.Clamp01(this._battery - num2);
			if (this._battery == 0f)
			{
				this._enabled = false;
			}
			if (Mathf.Abs((int)(this._lastSentBatteryLevel - this.BatteryPercent)) >= 1 && base.OwnerInventory.CurItem.TypeId == ItemType.Radio)
			{
				this.SendStatusMessage();
			}
			PlayerEvents.OnUsedRadio(new PlayerUsedRadioEventArgs(base.Owner, this, num2));
		}

		private void SendStatusMessage()
		{
			this._lastSentBatteryLevel = this.BatteryPercent;
			NetworkServer.SendToReady<RadioStatusMessage>(new RadioStatusMessage(this), 0);
		}

		private const float DrainMultiplier = 0.5f;

		public RadioRangeMode[] Ranges;

		public AnimationCurve VoiceVolumeCurve;

		public AnimationCurve NoiseLevelCurve;

		private bool _enabled;

		private float _battery;

		private byte _lastSentBatteryLevel;

		private byte _rangeId;

		private static KeyCode _circleModeKey;

		private static KeyCode _toggleKey;
	}
}
