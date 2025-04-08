using System;
using AudioPooling;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ScannerTracker : Scp079AbilityBase
	{
		public event Action<ReferenceHub> OnDetected;

		public string StatusText
		{
			get
			{
				if (this._sequence.SequencePaused)
				{
					return this._statusDisabled;
				}
				int num = Mathf.CeilToInt(this._sequence.RemainingTime);
				if (num > 0)
				{
					return string.Format(this._statusNextScan, num);
				}
				return this._statusScanning;
			}
		}

		private void AddTarget(ReferenceHub hub)
		{
			int num = this.TrackedPlayers.Length;
			for (int i = 0; i < num; i++)
			{
				if (this.TrackedPlayers[i] == null)
				{
					this.TrackedPlayers[i] = new Scp079ScannerTrackedPlayer(hub);
					return;
				}
			}
			Array.Resize<Scp079ScannerTrackedPlayer>(ref this.TrackedPlayers, num + 32);
			this.TrackedPlayers[num] = new Scp079ScannerTrackedPlayer(hub);
		}

		private void RemoveTarget(ReferenceHub hub)
		{
			int num = this.TrackedPlayers.Length;
			int hashCode = hub.GetHashCode();
			for (int i = 0; i < num; i++)
			{
				Scp079ScannerTrackedPlayer scp079ScannerTrackedPlayer = this.TrackedPlayers[i];
				if (scp079ScannerTrackedPlayer != null && scp079ScannerTrackedPlayer.PlayerHash == hashCode)
				{
					this.TrackedPlayers[i] = null;
					return;
				}
			}
		}

		private void OnRoleChanged(ReferenceHub ply, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (prevRole is HumanRole)
			{
				this.RemoveTarget(ply);
			}
			if (newRole is HumanRole)
			{
				this.AddTarget(ply);
			}
		}

		private void Update()
		{
			if (!this._sequenceActive || !NetworkServer.active)
			{
				return;
			}
			int num = this.TrackedPlayers.Length;
			for (int i = 0; i < num; i++)
			{
				int num2 = this._lastRefreshedIndex + 1;
				this._lastRefreshedIndex = num2;
				int num3 = num2 % num;
				Scp079ScannerTrackedPlayer scp079ScannerTrackedPlayer = this.TrackedPlayers[num3];
				if (scp079ScannerTrackedPlayer != null)
				{
					this._lastRefreshedIndex = num3;
					scp079ScannerTrackedPlayer.Update(this._areaBaselineRadius, this._areaAdditiveRadius, this._maxCampingTime);
					break;
				}
			}
			bool flag;
			this._sequence.ServerUpdate(out flag);
			if (flag)
			{
				base.ServerSendRpc(true);
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this._sequence.WriteRpc(writer);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._sequence.ReadRpc(reader);
		}

		protected override void Awake()
		{
			base.Awake();
			this._statusScanning = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanStatusScanning);
			this._statusNextScan = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanStatusWaiting);
			this._statusDisabled = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScanStatusDisabled);
		}

		internal void ClientProcessScanResult(ReferenceHub ply, int nextScan, NetworkReader data)
		{
			if (ply != null)
			{
				HumanRole humanRole = ply.roleManager.CurrentRole as HumanRole;
				if (humanRole != null)
				{
					Scp079NotificationManager.AddNotification(new Scp079ScannerNotification(humanRole));
					AudioSourcePoolManager.PlayAtPosition(this._alarmSound, data.ReadRelativePosition(), this._alarmRange, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f).Source.transform.position += Vector3.up * this._alarmHeight;
					Action<ReferenceHub> onDetected = this.OnDetected;
					if (onDetected == null)
					{
						return;
					}
					onDetected(ply);
					return;
				}
			}
			Scp079NotificationManager.AddNotification(new Scp079ScannerNotification(nextScan));
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._sequence = new Scp079ScannerSequence(base.CastRole);
			if (!NetworkServer.active)
			{
				return;
			}
			this._sequenceActive = true;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.RemoveTarget));
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.roleManager.CurrentRole is HumanRole)
				{
					this.AddTarget(referenceHub);
				}
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			if (!this._sequenceActive)
			{
				return;
			}
			this._sequenceActive = false;
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.RemoveTarget));
			Array.Clear(this.TrackedPlayers, 0, this.TrackedPlayers.Length);
		}

		private const int InitialTrackerSize = 32;

		private Scp079ScannerSequence _sequence;

		private int _lastRefreshedIndex;

		private bool _sequenceActive;

		private string _statusScanning;

		private string _statusNextScan;

		private string _statusDisabled;

		[SerializeField]
		private float _sequenceTime;

		[SerializeField]
		private float _warningTime;

		[SerializeField]
		private float _maxCampingTime;

		[SerializeField]
		private float _areaBaselineRadius;

		[SerializeField]
		private float _areaAdditiveRadius;

		[SerializeField]
		private float _addZonesPenalty;

		[SerializeField]
		private float _scannedEffectDuration;

		[SerializeField]
		private AudioClip _alarmSound;

		[SerializeField]
		private float _alarmHeight;

		[SerializeField]
		private float _alarmRange;

		public Scp079ScannerTrackedPlayer[] TrackedPlayers = new Scp079ScannerTrackedPlayer[32];
	}
}
