using System;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.Map;
using UnityEngine;
using VoiceChat;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079SpeakerAbility : Scp079KeyAbilityBase, IScp079AuxRegenModifier
	{
		public float AuxRegenMultiplier
		{
			get
			{
				if (!this.IsUsingSpeaker)
				{
					return 1f;
				}
				return this._regenMultiplier;
			}
		}

		public string AuxReductionMessage { get; private set; }

		public bool CanTransmit
		{
			get
			{
				return !base.LostSignalHandler.Lost;
			}
		}

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.AltVoiceChat;
			}
		}

		public override bool IsReady
		{
			get
			{
				return true;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return !Scp079ToggleMenuAbilityBase<Scp079MapToggler>.Visible && this.CanTransmit;
			}
		}

		public override string AbilityName
		{
			get
			{
				return this._abilityName;
			}
		}

		public override string FailMessage
		{
			get
			{
				return null;
			}
		}

		protected override void Trigger()
		{
		}

		private bool IsUsingSpeaker
		{
			get
			{
				if (!NetworkServer.active)
				{
					return this._syncUsing;
				}
				bool serverIsSending = this._voiceModule.ServerIsSending;
				bool flag = VoiceChatMutes.IsMuted(base.Owner, false);
				VoiceChatChannel currentChannel = this._voiceModule.CurrentChannel;
				return serverIsSending && !flag && currentChannel == VoiceChatChannel.Proximity;
			}
		}

		private void RefreshNearestSpeaker()
		{
			Scp079Camera scp079Camera;
			if (!base.CurrentCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			Scp079Speaker scp079Speaker;
			if (!Scp079Speaker.TryGetSpeaker(scp079Camera, out scp079Speaker))
			{
				return;
			}
			this._voiceModule.ProximityPlayback.transform.position = scp079Speaker.Position;
		}

		protected override void Awake()
		{
			base.Awake();
			this._abilityName = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.UseSpeaker);
			this.AuxReductionMessage = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.SpeakerAuxPause);
			base.CurrentCamSync.OnCameraChanged += this.RefreshNearestSpeaker;
		}

		protected override void Update()
		{
			base.Update();
			if (!NetworkServer.active)
			{
				return;
			}
			if (this._syncUsing == this.IsUsingSpeaker)
			{
				return;
			}
			base.ServerSendRpc(true);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._voiceModule = base.CastRole.VoiceModule as Scp079VoiceModule;
			this.RefreshNearestSpeaker();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._syncUsing = false;
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBool(this.IsUsingSpeaker);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._syncUsing = reader.ReadBool();
		}

		[SerializeField]
		private float _regenMultiplier;

		private bool _syncUsing;

		private string _abilityName;

		private Scp079VoiceModule _voiceModule;
	}
}
