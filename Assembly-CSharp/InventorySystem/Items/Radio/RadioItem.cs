using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Scp914;
using UnityEngine;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio;

public class RadioItem : ItemBase, IAcquisitionConfirmationTrigger, IItemDescription, IItemNametag, IUpgradeTrigger, IUniqueItem
{
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

	public bool AcquisitionAlreadyReceived { get; set; }

	public override float Weight => 1.7f;

	public bool IsUsable
	{
		get
		{
			if (this._enabled)
			{
				return this._battery > 0f;
			}
			return false;
		}
	}

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	public byte BatteryPercent
	{
		get
		{
			return (byte)Mathf.RoundToInt(this._battery * 100f);
		}
		set
		{
			this._battery = (float)(int)value / 100f;
		}
	}

	public RadioMessages.RadioRangeLevel RangeLevel => (RadioMessages.RadioRangeLevel)(this._enabled ? this._rangeId : (-1));

	public void ServerConfirmAcqusition()
	{
		this.SendStatusMessage();
	}

	public bool CompareIdentical(ItemBase other)
	{
		if (other is RadioItem radioItem && this.RangeLevel == radioItem.RangeLevel)
		{
			return this.BatteryPercent == radioItem.BatteryPercent;
		}
		return false;
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
			RadioItem._circleModeKey = NewInput.GetKey(ActionName.Shoot);
			RadioItem._toggleKey = NewInput.GetKey(ActionName.Zoom);
		}
		if (NetworkServer.active)
		{
			if (ipb is RadioPickup radioPickup)
			{
				this._enabled = radioPickup.SavedEnabled;
				this._rangeId = radioPickup.SavedRange;
				this._battery = radioPickup.SavedBattery;
			}
			else
			{
				this._enabled = true;
				this._battery = 1f;
				this._rangeId = 1;
			}
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		if (NetworkServer.active && pickup is RadioPickup radioPickup)
		{
			radioPickup.NetworkSavedEnabled = this._enabled;
			radioPickup.NetworkSavedRange = this._rangeId;
			radioPickup.SavedBattery = this._battery;
		}
	}

	public override void OnEquipped()
	{
		if (NetworkServer.active)
		{
			this.SendStatusMessage();
		}
	}

	public override void EquipUpdate()
	{
		if (this.IsLocalPlayer && InventoryGuiController.ItemsSafeForInteraction && !Cursor.visible)
		{
			if (Input.GetKeyDown(RadioItem._circleModeKey) && this._enabled)
			{
				NetworkClient.Send(new ClientRadioCommandMessage(RadioMessages.RadioCommand.ChangeRange));
			}
			if (Input.GetKeyDown(RadioItem._toggleKey))
			{
				NetworkClient.Send(new ClientRadioCommandMessage(this._enabled ? RadioMessages.RadioCommand.Disable : RadioMessages.RadioCommand.Enable));
			}
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
			PlayerTogglingRadioEventArgs e3 = new PlayerTogglingRadioEventArgs(base.Owner, this, newState: true);
			PlayerEvents.OnTogglingRadio(e3);
			if (!e3.IsAllowed)
			{
				return;
			}
			this._enabled = true;
			PlayerEvents.OnToggledRadio(new PlayerToggledRadioEventArgs(base.Owner, this, newState: true));
			break;
		}
		case RadioMessages.RadioCommand.Disable:
		{
			if (!this._enabled)
			{
				return;
			}
			PlayerTogglingRadioEventArgs e2 = new PlayerTogglingRadioEventArgs(base.Owner, this, newState: false);
			PlayerEvents.OnTogglingRadio(e2);
			if (!e2.IsAllowed)
			{
				return;
			}
			this._enabled = false;
			PlayerEvents.OnToggledRadio(new PlayerToggledRadioEventArgs(base.Owner, this, newState: false));
			break;
		}
		case RadioMessages.RadioCommand.ChangeRange:
		{
			byte b = (byte)(this._rangeId + 1);
			if (b >= this.Ranges.Length)
			{
				b = 0;
			}
			PlayerChangingRadioRangeEventArgs e = new PlayerChangingRadioRangeEventArgs(base.Owner, this, (RadioMessages.RadioRangeLevel)b);
			PlayerEvents.OnChangingRadioRange(e);
			if (!e.IsAllowed)
			{
				return;
			}
			PlayerEvents.OnChangedRadioRange(new PlayerChangedRadioRangeEventArgs(range: (RadioMessages.RadioRangeLevel)(this._rangeId = (byte)e.Range), player: base.Owner, radio: this));
			break;
		}
		}
		this.SendStatusMessage();
	}

	public void UserReceiveInfo(RadioStatusMessage info)
	{
		if (this.IsLocalPlayer)
		{
			this._enabled = info.Range != RadioMessages.RadioRangeLevel.RadioDisabled;
			this.BatteryPercent = info.Battery;
		}
	}

	private void Update()
	{
		if (!NetworkServer.active || !this.IsUsable)
		{
			return;
		}
		float num = (PersonalRadioPlayback.IsTransmitting(base.Owner) ? ((float)this.Ranges[this._rangeId].MinuteCostWhenTalking) : this.Ranges[this._rangeId].MinuteCostWhenIdle);
		float drain = Time.deltaTime * 0.5f * (num / 60f / 100f);
		PlayerUsingRadioEventArgs e = new PlayerUsingRadioEventArgs(base.Owner, this, drain);
		PlayerEvents.OnUsingRadio(e);
		if (e.IsAllowed)
		{
			drain = e.Drain;
			this._battery = Mathf.Clamp01(this._battery - drain);
			if (this._battery == 0f)
			{
				this._enabled = false;
			}
			if (Mathf.Abs(this._lastSentBatteryLevel - this.BatteryPercent) >= 1 && base.OwnerInventory.CurItem.TypeId == ItemType.Radio)
			{
				this.SendStatusMessage();
			}
			PlayerEvents.OnUsedRadio(new PlayerUsedRadioEventArgs(base.Owner, this, drain));
		}
	}

	private void SendStatusMessage()
	{
		this._lastSentBatteryLevel = this.BatteryPercent;
		NetworkServer.SendToReady(new RadioStatusMessage(this));
	}
}
