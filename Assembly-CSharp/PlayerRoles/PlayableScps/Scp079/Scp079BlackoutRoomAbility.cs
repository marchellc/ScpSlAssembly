using System;
using System.Collections.Generic;
using System.Text;
using AudioPooling;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079BlackoutRoomAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier
	{
		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079Blackout;
			}
		}

		public override bool IsReady
		{
			get
			{
				return this.ErrorCode == Scp079HudTranslation.Zoom;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return this.CurrentCapacity > 0 && !Scp079CursorManager.LockCameras;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._nameFormat, this.AbilityCost);
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
				Scp079HudTranslation errorCode = this.ErrorCode;
				if (errorCode <= Scp079HudTranslation.NotEnoughAux)
				{
					if (errorCode == Scp079HudTranslation.Zoom)
					{
						return null;
					}
					if (errorCode == Scp079HudTranslation.NotEnoughAux)
					{
						return base.GetNoAuxMessage(this.AbilityCost);
					}
				}
				else
				{
					if (errorCode == Scp079HudTranslation.BlackoutRoomLimit)
					{
						return string.Format(this._failMessage, this.RoomsOnCooldown, this.CurrentCapacity);
					}
					if (errorCode == Scp079HudTranslation.BlackoutRoomCooldown)
					{
						return this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this.RemainingCooldown));
					}
				}
				return this._failMessage;
			}
		}

		public AudioClip ConfirmationSound { get; private set; }

		private int CurrentCapacity
		{
			get
			{
				return this.GetCapacityOfTier(base.TierManager.AccessTierIndex);
			}
		}

		private int RoomsOnCooldown
		{
			get
			{
				int num = 0;
				bool flag = false;
				foreach (KeyValuePair<uint, double> keyValuePair in this._blackoutCooldowns)
				{
					if (keyValuePair.Value < NetworkTime.time)
					{
						this._obsoleteCooldowns.Add(keyValuePair.Key);
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
				foreach (uint num2 in this._obsoleteCooldowns)
				{
					this._blackoutCooldowns.Remove(num2);
				}
				this._obsoleteCooldowns.Clear();
				return num;
			}
		}

		private float RemainingCooldown
		{
			get
			{
				double num;
				if (!this._hasController || !this._blackoutCooldowns.TryGetValue(this._roomController.netId, out num))
				{
					return 0f;
				}
				double num2 = num - NetworkTime.time;
				return Mathf.Max(0f, (float)num2);
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

		private bool IsOnSurface
		{
			get
			{
				return base.CurrentCamSync.CurrentCamera.Room.Zone == FacilityZone.Surface;
			}
		}

		private float AbilityCost
		{
			get
			{
				return (float)(this.IsOnSurface ? this._surfaceCost : this._cost);
			}
		}

		private void RefreshCurrentController()
		{
			this._hasController = false;
			this._hasFailMessage = false;
			this._failMessage = null;
			RoomIdentifier room = base.CurrentCamSync.CurrentCamera.Room;
			foreach (RoomLightController roomLightController in RoomLightController.Instances)
			{
				if (!(roomLightController.Room != room))
				{
					float y = base.CurrentCamSync.CurrentCamera.Position.y;
					float y2 = roomLightController.transform.position.y;
					if (Mathf.Abs(y - y2) <= 100f)
					{
						this._roomController = roomLightController;
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
			this._nameFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ActivateRoomBlackout);
			this._textUnlock = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.BlackoutRoomAvailable);
			this._textCapacityIncreased = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.BlackoutCapacityIncreased);
			base.CurrentCamSync.OnCameraChanged += this.RefreshCurrentController;
		}

		protected override void Trigger()
		{
			base.ClientSendCmd();
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.IsReady || base.LostSignalHandler.Lost)
			{
				this._successfulController = null;
				base.ServerSendRpc(false);
				return;
			}
			Scp079BlackingOutRoomEventsArgs scp079BlackingOutRoomEventsArgs = new Scp079BlackingOutRoomEventsArgs(base.Owner, this._roomController.Room);
			Scp079Events.OnBlackingOutRoom(scp079BlackingOutRoomEventsArgs);
			if (!scp079BlackingOutRoomEventsArgs.IsAllowed)
			{
				return;
			}
			base.AuxManager.CurrentAux -= this.AbilityCost;
			base.RewardManager.MarkRoom(this._roomController.Room);
			this._blackoutCooldowns[this._roomController.netId] = NetworkTime.time + (double)this._cooldown;
			this._roomController.ServerFlickerLights(this._blackoutDuration);
			this._successfulController = this._roomController;
			base.ServerSendRpc(true);
			Scp079Events.OnBlackedOutRoom(new Scp079BlackedOutRoomEventArgs(base.Owner, this._roomController.Room));
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteNetworkBehaviour(this._successfulController);
			writer.WriteByte((byte)this.RoomsOnCooldown);
			foreach (KeyValuePair<uint, double> keyValuePair in this._blackoutCooldowns)
			{
				writer.WriteUInt(keyValuePair.Key);
				writer.WriteDouble(keyValuePair.Value);
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
			int num = (int)reader.ReadByte();
			this._blackoutCooldowns.Clear();
			for (int i = 0; i < num; i++)
			{
				uint num2 = reader.ReadUInt();
				double num3 = reader.ReadDouble();
				this._blackoutCooldowns.Add(num2, num3);
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
			Vector3 vector = flc.transform.position + Vector3.down * 15f;
			AudioSourcePoolManager.PlayAtPosition(this.ConfirmationSound, vector, 37f, 1f, FalloffType.Linear, MixerChannel.DefaultSfx, 1f).Source.minDistance = 15f;
		}

		public override void OnFailMessageAssigned()
		{
			base.OnFailMessageAssigned();
			this._hasFailMessage = true;
			this._failMessage = Translations.Get<Scp079HudTranslation>(this.ErrorCode);
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
				sb.AppendFormat(this._textUnlock, string.Format("[{0}]", new ReadableKeyCode(this.ActivationKey)));
			}
			return true;
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

		private enum ValidationError
		{
			None,
			NotEnoughAux,
			NoController = 26,
			MaxCapacityReached,
			RoomOnCooldown,
			AlreadyBlackedOut = 60
		}
	}
}
