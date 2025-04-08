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

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079BlackoutZoneAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier
	{
		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		public override bool IsReady
		{
			get
			{
				return this.ErrorCode == Scp079HudTranslation.Zoom;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._nameFormat, this._cost);
			}
		}

		public override bool IsVisible
		{
			get
			{
				return Scp079Role.LocalInstanceActive && this.Unlocked && !Cursor.visible && Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen && this._syncZone > FacilityZone.None;
			}
		}

		public bool Unlocked
		{
			get
			{
				return base.TierManager.AccessTierIndex >= this._minTierIndex;
			}
		}

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
				Scp079HudTranslation failReason = this._failReason;
				if (failReason == Scp079HudTranslation.Zoom)
				{
					return null;
				}
				if (failReason != Scp079HudTranslation.NotEnoughAux)
				{
					if (failReason != Scp079HudTranslation.ZoneBlackoutCooldown)
					{
						return this._failMessage;
					}
					if (this._cooldownTimer.IsReady)
					{
						return null;
					}
					return this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this._cooldownTimer.Remaining));
				}
				else
				{
					if (base.AuxManager.CurrentAuxFloored >= this._cost)
					{
						return null;
					}
					return base.GetNoAuxMessage((float)this._cost);
				}
			}
		}

		protected override void Start()
		{
			base.Start();
			this._nameFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ActivateZoneBlackout);
			this._textUnlock = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ZoneBlackoutAvailable);
		}

		protected override void Update()
		{
			base.Update();
			if (!base.Owner.isLocalPlayer)
			{
				return;
			}
			this._syncZone = ZoneBlackoutIcon.HighlightedZone;
		}

		public override void OnFailMessageAssigned()
		{
			base.OnFailMessageAssigned();
			this._hasFailMessage = true;
			this._failReason = this.ErrorCode;
			this._failMessage = Translations.Get<Scp079HudTranslation>(this._failReason);
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
			Scp079BlackingOutZoneEventArgs scp079BlackingOutZoneEventArgs = new Scp079BlackingOutZoneEventArgs(base.Owner, this._syncZone);
			Scp079Events.OnBlackingOutZone(scp079BlackingOutZoneEventArgs);
			if (!scp079BlackingOutZoneEventArgs.IsAllowed)
			{
				return;
			}
			foreach (RoomLightController roomLightController in RoomLightController.Instances)
			{
				if (roomLightController.Room.Zone == this._syncZone)
				{
					roomLightController.ServerFlickerLights(this._duration);
				}
			}
			this._cooldownTimer.Trigger((double)this._cooldown);
			base.AuxManager.CurrentAux -= (float)this._cost;
			base.ServerSendRpc(true);
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

		private enum ValidationError
		{
			None,
			NotEnoughAux,
			Cooldown = 59,
			Unavailable = 61
		}
	}
}
