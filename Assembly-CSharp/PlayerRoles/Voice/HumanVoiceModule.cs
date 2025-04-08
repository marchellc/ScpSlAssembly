using System;
using AudioPooling;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp1576;
using PlayerRoles.Visibility;
using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.Voice
{
	public class HumanVoiceModule : StandardVoiceModule, IRadioVoiceModule
	{
		private bool Transmitting
		{
			get
			{
				return this._wasTransmitting;
			}
			set
			{
				if (this.Transmitting == value)
				{
					return;
				}
				AudioSourcePoolManager.Play2D((value ? this._radioOnSounds : this._radioOffSounds).RandomItem<AudioClip>(), this._toggleSoundsVolume, MixerChannel.VoiceChat, 1f);
				this._wasTransmitting = value;
			}
		}

		public SingleBufferPlayback ProximityPlayback { get; private set; }

		public SingleBufferPlayback Scp1576Playback { get; private set; }

		public PersonalRadioPlayback RadioPlayback { get; private set; }

		public override bool IsSpeaking
		{
			get
			{
				return this.ProximityPlayback.MaxSamples > 0;
			}
		}

		private bool CheckProximity(ReferenceHub hub)
		{
			return hub != base.Owner && this._vctrl.ValidateVisibility(hub);
		}

		protected override VoiceChatChannel ProcessInputs(bool primary, bool alt)
		{
			if ((primary || alt) && Scp1576Item.LocallyUsed)
			{
				this.Transmitting = false;
				return VoiceChatChannel.Scp1576;
			}
			if (alt && this.RadioPlayback.RadioUsable && !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
			{
				this.Transmitting = true;
				return VoiceChatChannel.Radio;
			}
			this.Transmitting = false;
			if (!primary)
			{
				return VoiceChatChannel.None;
			}
			return VoiceChatChannel.Proximity;
		}

		protected override void ProcessSamples(float[] data, int len)
		{
			base.ProcessSamples(data, len);
			VoiceChatChannel currentChannel = base.CurrentChannel;
			if (currentChannel != VoiceChatChannel.Radio)
			{
				if (currentChannel == VoiceChatChannel.RoundSummary)
				{
					return;
				}
				if (currentChannel == VoiceChatChannel.Scp1576)
				{
					this.Scp1576Playback.Buffer.Write(data, len);
					return;
				}
			}
			else
			{
				this.RadioPlayback.DistributeSamples(data, len);
			}
			this.ProximityPlayback.Buffer.Write(data, len);
		}

		protected override void Update()
		{
			base.Update();
			ReferenceHub referenceHub;
			bool flag;
			if (this.IsSpeaking && base.CurrentChannel == VoiceChatChannel.Radio && ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				IVoiceRole voiceRole = referenceHub.roleManager.CurrentRole as IVoiceRole;
				if (voiceRole != null)
				{
					IRadioVoiceModule radioVoiceModule = voiceRole.VoiceModule as IRadioVoiceModule;
					if (radioVoiceModule != null)
					{
						flag = radioVoiceModule.RadioPlayback.RadioUsable;
						goto IL_0051;
					}
				}
			}
			flag = false;
			IL_0051:
			bool flag2 = flag;
			this.ProximityPlayback.Source.volume = (flag2 ? 0.35f : 1f);
		}

		protected override void Awake()
		{
			base.Awake();
			this._vctrl = (base.Role as ICustomVisibilityRole).VisibilityController;
		}

		public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
		{
			if (channel != VoiceChatChannel.Proximity)
			{
				if (channel != VoiceChatChannel.Radio)
				{
					if (channel == VoiceChatChannel.Scp1576)
					{
						if (!Scp1576Item.ValidatedReceivers.Contains(base.Owner))
						{
							return VoiceChatChannel.Proximity;
						}
						return VoiceChatChannel.Scp1576;
					}
				}
				else if (this.RadioPlayback.RadioUsable)
				{
					return channel;
				}
				return VoiceChatChannel.None;
			}
			return channel;
		}

		public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
		{
			VoiceChatChannel voiceChatChannel = base.ValidateReceive(speaker, channel);
			if (voiceChatChannel == VoiceChatChannel.Intercom || voiceChatChannel == VoiceChatChannel.RoundSummary)
			{
				return voiceChatChannel;
			}
			switch (channel)
			{
			case VoiceChatChannel.Proximity:
				if (!this.CheckProximity(speaker))
				{
					return VoiceChatChannel.None;
				}
				break;
			case VoiceChatChannel.Radio:
			case VoiceChatChannel.Mimicry:
				break;
			case VoiceChatChannel.ScpChat:
			case VoiceChatChannel.RoundSummary:
			case VoiceChatChannel.Intercom:
				return VoiceChatChannel.None;
			case VoiceChatChannel.Spectator:
				if (Scp1576Item.ValidatedReceivers.Contains(base.Owner))
				{
					return VoiceChatChannel.Scp1576;
				}
				return VoiceChatChannel.None;
			case VoiceChatChannel.Scp1576:
				return VoiceChatChannel.Proximity;
			default:
				return VoiceChatChannel.None;
			}
			return channel;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this.RadioPlayback.Setup(base.Owner, this.ProximityPlayback);
			this.ProximityPlayback.Source.mute = base.Owner.isLocalPlayer;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._wasTransmitting = false;
		}

		private const float RadioProximityRatio = 0.35f;

		[SerializeField]
		private AudioClip[] _radioOnSounds;

		[SerializeField]
		private AudioClip[] _radioOffSounds;

		[SerializeField]
		private float _toggleSoundsVolume;

		private VisibilityController _vctrl;

		private bool _wasTransmitting;
	}
}
