using System;
using System.Diagnostics;
using AudioPooling;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public abstract class Scp079ToggleMenuAbilityBase<T> : Scp079KeyAbilityBase where T : Scp079ToggleMenuAbilityBase<T>
	{
		public override bool IsReady
		{
			get
			{
				return Scp079ToggleMenuAbilityBase<T>.CooldownSw.Elapsed.TotalSeconds > (double)this._cooldown;
			}
		}

		public override string AbilityName
		{
			get
			{
				if (!this.SyncState)
				{
					return this._openTxt;
				}
				return this._closeTxt;
			}
		}

		public override string FailMessage
		{
			get
			{
				return null;
			}
		}

		protected bool SyncState { get; set; }

		protected abstract Scp079HudTranslation OpenTranslation { get; }

		protected abstract Scp079HudTranslation CloseTranslation { get; }

		public static bool IsOpen
		{
			get
			{
				return Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet && Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState;
			}
			internal set
			{
				if (!Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet || Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState == value)
				{
					return;
				}
				Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState = value;
				Scp079ToggleMenuAbilityBase<T>.CooldownSw.Restart();
				if (!Scp079ToggleMenuAbilityBase<T>._trackedInstance.Role.IsLocalPlayer)
				{
					return;
				}
				Scp079ToggleMenuAbilityBase<T>._trackedInstance.ClientSendCmd();
			}
		}

		public static bool Visible
		{
			get
			{
				return Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet && (Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState || !Scp079ToggleMenuAbilityBase<T>._trackedInstance.IsReady);
			}
		}

		private void OnSpectatorTargetChanged()
		{
			if (base.CastRole.IsSpectated || base.CastRole.IsLocalPlayer)
			{
				Scp079ToggleMenuAbilityBase<T>._trackedInstance = this;
				Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = true;
				return;
			}
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = false;
		}

		private void PlaySound()
		{
			AudioSourcePoolManager.Play2D(this.SyncState ? this._soundOpen : this._soundClose, 1f, MixerChannel.DefaultSfx, 1f);
		}

		protected override void Trigger()
		{
			if (base.CurrentCamSync.CurClientSwitchState != Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				return;
			}
			Scp079ToggleMenuAbilityBase<T>.IsOpen = !Scp079ToggleMenuAbilityBase<T>.IsOpen;
			this.PlaySound();
		}

		protected override void Start()
		{
			base.Start();
			this._openTxt = Translations.Get<Scp079HudTranslation>(this.OpenTranslation);
			this._closeTxt = Translations.Get<Scp079HudTranslation>(this.CloseTranslation);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteBool(this.SyncState);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this.SyncState = reader.ReadBool();
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBool(this.SyncState);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			bool syncState = this.SyncState;
			this.SyncState = reader.ReadBool();
			if (syncState == this.SyncState || !base.Owner.IsLocallySpectated())
			{
				return;
			}
			Scp079CurrentCameraSync scp079CurrentCameraSync;
			if (!base.CastRole.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out scp079CurrentCameraSync))
			{
				return;
			}
			if (scp079CurrentCameraSync.CurClientSwitchState != Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				return;
			}
			this.PlaySound();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(base.ServerSendRpc));
			SpectatorTargetTracker.OnTargetChanged += this.OnSpectatorTargetChanged;
			if (!base.Role.IsLocalPlayer)
			{
				return;
			}
			Scp079ToggleMenuAbilityBase<T>._trackedInstance = this;
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = true;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(base.ServerSendRpc));
			SpectatorTargetTracker.OnTargetChanged -= this.OnSpectatorTargetChanged;
			this.SyncState = false;
			if (!Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet || this != Scp079ToggleMenuAbilityBase<T>._trackedInstance)
			{
				return;
			}
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = false;
		}

		[SerializeField]
		private float _cooldown;

		[SerializeField]
		private AudioClip _soundOpen;

		[SerializeField]
		private AudioClip _soundClose;

		private string _openTxt;

		private string _closeTxt;

		private static bool _trackedInstanceSet;

		private static Scp079ToggleMenuAbilityBase<T> _trackedInstance;

		private static readonly Stopwatch CooldownSw = Stopwatch.StartNew();
	}
}
