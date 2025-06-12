using System;
using System.Text;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Map;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079BlackoutZoneAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier
{
	private enum ValidationError
	{
		None = 0,
		NotEnoughAux = 1,
		Cooldown = 59,
		Unavailable = 61
	}

	public static Action<ReferenceHub, FacilityZone> OnClientZoneBlackout;

	[SerializeField]
	private int _cost;

	[SerializeField]
	private float _duration;

	[SerializeField]
	private float _cooldown;

	[SerializeField]
	private int _minTierIndex;

	[SerializeField]
	private FacilityZone[] _availableZones;

	private readonly AbilityCooldown _cooldownTimer = new AbilityCooldown();

	private string _nameFormat;

	private string _textUnlock;

	private string _failMessage;

	private bool _hasFailMessage;

	private FacilityZone _syncZone;

	private Scp079HudTranslation _failReason;

	public override ActionName ActivationKey => ActionName.Shoot;

	public override bool IsReady => this.ErrorCode == Scp079HudTranslation.Zoom;

	public override string AbilityName => string.Format(this._nameFormat, this._cost);

	public override bool IsVisible
	{
		get
		{
			if (Scp079Role.LocalInstanceActive && this.Unlocked && !Cursor.visible && Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen)
			{
				return this._syncZone != FacilityZone.None;
			}
			return false;
		}
	}

	public bool Unlocked => base.TierManager.AccessTierIndex >= this._minTierIndex;

	private Scp079HudTranslation ErrorCode
	{
		get
		{
			if (!this.Unlocked)
			{
				return Scp079HudTranslation.ZoneBlackoutUnavailable;
			}
			if (!this._availableZones.Contains(this._syncZone))
			{
				return Scp079HudTranslation.ZoneBlackoutUnavailable;
			}
			if (!this._cooldownTimer.IsReady)
			{
				return Scp079HudTranslation.ZoneBlackoutCooldown;
			}
			if (base.AuxManager.CurrentAuxFloored < this._cost)
			{
				return Scp079HudTranslation.NotEnoughAux;
			}
			return Scp079HudTranslation.Zoom;
		}
	}

	public override string FailMessage
	{
		get
		{
			if (!this._hasFailMessage)
			{
				return null;
			}
			switch (this._failReason)
			{
			case Scp079HudTranslation.Zoom:
				return null;
			case Scp079HudTranslation.NotEnoughAux:
				if (base.AuxManager.CurrentAuxFloored >= this._cost)
				{
					return null;
				}
				return base.GetNoAuxMessage(this._cost);
			case Scp079HudTranslation.ZoneBlackoutCooldown:
				if (this._cooldownTimer.IsReady)
				{
					return null;
				}
				return this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this._cooldownTimer.Remaining));
			default:
				return this._failMessage;
			}
		}
	}

	protected override void Start()
	{
		base.Start();
		this._nameFormat = Translations.Get(Scp079HudTranslation.ActivateZoneBlackout);
		this._textUnlock = Translations.Get(Scp079HudTranslation.ZoneBlackoutAvailable);
	}

	protected override void Update()
	{
		base.Update();
		if (base.Owner.isLocalPlayer)
		{
			this._syncZone = ZoneBlackoutIcon.HighlightedZone;
		}
	}

	public override void OnFailMessageAssigned()
	{
		base.OnFailMessageAssigned();
		this._hasFailMessage = true;
		this._failReason = this.ErrorCode;
		this._failMessage = Translations.Get(this._failReason);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._cooldownTimer.Clear();
	}

	public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
	{
		if (newLevel != this._minTierIndex)
		{
			return false;
		}
		sb.Append(this._textUnlock);
		return true;
	}

	protected override void Trigger()
	{
		base.ClientSendCmd();
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteByte((byte)this._syncZone);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this._syncZone = (FacilityZone)reader.ReadByte();
		if (this.ErrorCode != Scp079HudTranslation.Zoom)
		{
			return;
		}
		Scp079BlackingOutZoneEventArgs e = new Scp079BlackingOutZoneEventArgs(base.Owner, this._syncZone);
		Scp079Events.OnBlackingOutZone(e);
		if (!e.IsAllowed)
		{
			return;
		}
		foreach (RoomLightController instance in RoomLightController.Instances)
		{
			if (instance.Room.Zone == this._syncZone)
			{
				instance.ServerFlickerLights(this._duration);
			}
		}
		this._cooldownTimer.Trigger(this._cooldown);
		base.AuxManager.CurrentAux -= this._cost;
		base.ServerSendRpc(toAll: true);
		Scp079Events.OnBlackedOutZone(new Scp079BlackedOutZoneEventArgs(base.Owner, this._syncZone));
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._syncZone);
		this._cooldownTimer.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
	}
}
