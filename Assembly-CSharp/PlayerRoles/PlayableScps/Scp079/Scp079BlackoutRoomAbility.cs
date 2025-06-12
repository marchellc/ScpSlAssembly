using System.Collections.Generic;
using System.Text;
using AudioPooling;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079BlackoutRoomAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier
{
	private enum ValidationError
	{
		None = 0,
		NotEnoughAux = 1,
		NoController = 26,
		MaxCapacityReached = 27,
		RoomOnCooldown = 28,
		AlreadyBlackedOut = 60
	}

	[SerializeField]
	private int[] _capacityPerTier;

	[SerializeField]
	private float _blackoutDuration;

	[SerializeField]
	private float _cooldown;

	[SerializeField]
	private int _cost;

	[SerializeField]
	private int _surfaceCost;

	private string _textUnlock;

	private string _textCapacityIncreased;

	private string _nameFormat;

	private string _failMessage;

	private bool _hasFailMessage;

	private bool _hasController;

	private RoomLightController _successfulController;

	private RoomLightController _roomController;

	private readonly Dictionary<uint, double> _blackoutCooldowns = new Dictionary<uint, double>();

	private readonly HashSet<uint> _obsoleteCooldowns = new HashSet<uint>();

	public override ActionName ActivationKey => ActionName.Scp079Blackout;

	public override bool IsReady => this.ErrorCode == Scp079HudTranslation.Zoom;

	public override bool IsVisible
	{
		get
		{
			if (this.CurrentCapacity > 0)
			{
				return !Scp079CursorManager.LockCameras;
			}
			return false;
		}
	}

	public override string AbilityName => string.Format(this._nameFormat, this.AbilityCost);

	public override bool DummyEmulationSupport => true;

	public override string FailMessage
	{
		get
		{
			if (!this._hasFailMessage)
			{
				return null;
			}
			return this.ErrorCode switch
			{
				Scp079HudTranslation.Zoom => null, 
				Scp079HudTranslation.NotEnoughAux => base.GetNoAuxMessage(this.AbilityCost), 
				Scp079HudTranslation.BlackoutRoomCooldown => this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this.RemainingCooldown)), 
				Scp079HudTranslation.BlackoutRoomLimit => string.Format(this._failMessage, this.RoomsOnCooldown, this.CurrentCapacity), 
				_ => this._failMessage, 
			};
		}
	}

	[field: SerializeField]
	public AudioClip ConfirmationSound { get; private set; }

	private int CurrentCapacity => this.GetCapacityOfTier(base.TierManager.AccessTierIndex);

	private int RoomsOnCooldown
	{
		get
		{
			int num = 0;
			bool flag = false;
			foreach (KeyValuePair<uint, double> blackoutCooldown in this._blackoutCooldowns)
			{
				if (blackoutCooldown.Value < NetworkTime.time)
				{
					this._obsoleteCooldowns.Add(blackoutCooldown.Key);
					flag = true;
				}
				else
				{
					num++;
				}
			}
			if (!flag)
			{
				return num;
			}
			foreach (uint obsoleteCooldown in this._obsoleteCooldowns)
			{
				this._blackoutCooldowns.Remove(obsoleteCooldown);
			}
			this._obsoleteCooldowns.Clear();
			return num;
		}
	}

	private float RemainingCooldown
	{
		get
		{
			if (!this._hasController || !this._blackoutCooldowns.TryGetValue(this._roomController.netId, out var value))
			{
				return 0f;
			}
			double num = value - NetworkTime.time;
			return Mathf.Max(0f, (float)num);
		}
	}

	private Scp079HudTranslation ErrorCode
	{
		get
		{
			if (!this._hasController)
			{
				return Scp079HudTranslation.BlackoutRoomUnavailable;
			}
			if (!this._roomController.LightsEnabled)
			{
				return Scp079HudTranslation.BlackoutAlreadyActive;
			}
			if (this.RemainingCooldown > 0f)
			{
				return Scp079HudTranslation.BlackoutRoomCooldown;
			}
			if (this.RoomsOnCooldown >= this.CurrentCapacity)
			{
				return Scp079HudTranslation.BlackoutRoomLimit;
			}
			if ((float)base.AuxManager.CurrentAuxFloored < this.AbilityCost)
			{
				return Scp079HudTranslation.NotEnoughAux;
			}
			return Scp079HudTranslation.Zoom;
		}
	}

	private bool IsOnSurface => base.CurrentCamSync.CurrentCamera.Room.Zone == FacilityZone.Surface;

	private float AbilityCost => this.IsOnSurface ? this._surfaceCost : this._cost;

	private void RefreshCurrentController()
	{
		this._hasController = false;
		this._hasFailMessage = false;
		this._failMessage = null;
		RoomIdentifier room = base.CurrentCamSync.CurrentCamera.Room;
		foreach (RoomLightController instance in RoomLightController.Instances)
		{
			if (!(instance.Room != room))
			{
				float y = base.CurrentCamSync.CurrentCamera.Position.y;
				float y2 = instance.transform.position.y;
				if (!(Mathf.Abs(y - y2) > 50f))
				{
					this._roomController = instance;
					this._hasController = true;
					break;
				}
			}
		}
	}

	private int GetCapacityOfTier(int index)
	{
		index = Mathf.Clamp(index, 0, this._capacityPerTier.Length - 1);
		return this._capacityPerTier[index];
	}

	protected override void Start()
	{
		base.Start();
		this._nameFormat = Translations.Get(Scp079HudTranslation.ActivateRoomBlackout);
		this._textUnlock = Translations.Get(Scp079HudTranslation.BlackoutRoomAvailable);
		this._textCapacityIncreased = Translations.Get(Scp079HudTranslation.BlackoutCapacityIncreased);
		base.CurrentCamSync.OnCameraChanged += RefreshCurrentController;
	}

	protected override void Trigger()
	{
		base.ClientSendCmd();
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.IsReady && !base.LostSignalHandler.Lost)
		{
			Scp079BlackingOutRoomEventsArgs scp079BlackingOutRoomEventsArgs = new Scp079BlackingOutRoomEventsArgs(base.Owner, this._roomController.Room);
			Scp079Events.OnBlackingOutRoom(scp079BlackingOutRoomEventsArgs);
			if (scp079BlackingOutRoomEventsArgs.IsAllowed)
			{
				base.AuxManager.CurrentAux -= this.AbilityCost;
				base.RewardManager.MarkRoom(this._roomController.Room);
				this._blackoutCooldowns[this._roomController.netId] = NetworkTime.time + (double)this._cooldown;
				this._roomController.ServerFlickerLights(this._blackoutDuration);
				this._successfulController = this._roomController;
				base.ServerSendRpc(toAll: true);
				Scp079Events.OnBlackedOutRoom(new Scp079BlackedOutRoomEventArgs(base.Owner, this._roomController.Room));
			}
		}
		else
		{
			this._successfulController = null;
			base.ServerSendRpc(toAll: false);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteNetworkBehaviour(this._successfulController);
		writer.WriteByte((byte)this.RoomsOnCooldown);
		foreach (KeyValuePair<uint, double> blackoutCooldown in this._blackoutCooldowns)
		{
			writer.WriteUInt(blackoutCooldown.Key);
			writer.WriteDouble(blackoutCooldown.Value);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._successfulController = reader.ReadNetworkBehaviour<RoomLightController>();
		if (this._successfulController != null)
		{
			this.PlaySoundForController(this._successfulController);
		}
		int num = reader.ReadByte();
		this._blackoutCooldowns.Clear();
		for (int i = 0; i < num; i++)
		{
			uint key = reader.ReadUInt();
			double value = reader.ReadDouble();
			this._blackoutCooldowns.Add(key, value);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._blackoutCooldowns.Clear();
		this._obsoleteCooldowns.Clear();
	}

	public void PlaySoundForController(RoomLightController flc)
	{
		Vector3 position = flc.transform.position + Vector3.down * 15f;
		AudioSourcePoolManager.PlayAtPosition(this.ConfirmationSound, position, 37f, 1f, FalloffType.Linear).Source.minDistance = 15f;
	}

	public override void OnFailMessageAssigned()
	{
		base.OnFailMessageAssigned();
		this._hasFailMessage = true;
		this._failMessage = Translations.Get(this.ErrorCode);
	}

	public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
	{
		int capacityOfTier = this.GetCapacityOfTier(newLevel);
		int capacityOfTier2 = this.GetCapacityOfTier(newLevel - 1);
		if (capacityOfTier <= capacityOfTier2)
		{
			return false;
		}
		if (capacityOfTier2 > 0)
		{
			sb.AppendFormat(this._textCapacityIncreased, capacityOfTier);
		}
		else
		{
			sb.AppendFormat(this._textUnlock, $"[{new ReadableKeyCode(this.ActivationKey)}]");
		}
		return true;
	}
}
