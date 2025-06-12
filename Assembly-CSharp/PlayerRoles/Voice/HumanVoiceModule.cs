using AudioPooling;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp1576;
using PlayerRoles.Visibility;
using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.Voice;

public class HumanVoiceModule : StandardVoiceModule, IRadioVoiceModule
{
	private const float RadioProximityRatio = 0.35f;

	[SerializeField]
	private AudioClip[] _radioOnSounds;

	[SerializeField]
	private AudioClip[] _radioOffSounds;

	[SerializeField]
	private float _toggleSoundsVolume;

	private VisibilityController _vctrl;

	private bool _wasTransmitting;

	private bool Transmitting
	{
		get
		{
			return this._wasTransmitting;
		}
		set
		{
			if (this.Transmitting != value)
			{
				AudioSourcePoolManager.Play2D((value ? this._radioOnSounds : this._radioOffSounds).RandomItem(), this._toggleSoundsVolume, MixerChannel.VoiceChat);
				this._wasTransmitting = value;
			}
		}
	}

	public SingleBufferPlayback FirstProxPlayback => this.ProximityPlaybacks[0];

	[field: SerializeField]
	public SingleBufferPlayback[] ProximityPlaybacks { get; private set; }

	[field: SerializeField]
	public SingleBufferPlayback Scp1576Playback { get; private set; }

	[field: SerializeField]
	public PersonalRadioPlayback RadioPlayback { get; private set; }

	public override bool IsSpeaking => this.FirstProxPlayback.MaxSamples > 0;

	private bool CheckProximity(ReferenceHub hub)
	{
		if (hub != base.Owner)
		{
			return this._vctrl.ValidateVisibility(hub);
		}
		return false;
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
		switch (base.CurrentChannel)
		{
		case VoiceChatChannel.RoundSummary:
			return;
		case VoiceChatChannel.Scp1576:
			this.Scp1576Playback.Buffer.Write(data, len);
			return;
		case VoiceChatChannel.Radio:
			this.RadioPlayback.DistributeSamples(data, len);
			break;
		}
		SingleBufferPlayback[] proximityPlaybacks = this.ProximityPlaybacks;
		for (int i = 0; i < proximityPlaybacks.Length; i++)
		{
			proximityPlaybacks[i].Buffer.Write(data, len);
		}
	}

	protected override void Update()
	{
		base.Update();
		ReferenceHub hub;
		bool flag = this.IsSpeaking && base.CurrentChannel == VoiceChatChannel.Radio && ReferenceHub.TryGetLocalHub(out hub) && hub.roleManager.CurrentRole is IVoiceRole { VoiceModule: IRadioVoiceModule voiceModule } && voiceModule.RadioPlayback.RadioUsable;
		SingleBufferPlayback[] proximityPlaybacks = this.ProximityPlaybacks;
		for (int i = 0; i < proximityPlaybacks.Length; i++)
		{
			proximityPlaybacks[i].Source.volume = (flag ? 0.35f : 1f);
		}
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
				goto IL_0031;
			}
			return VoiceChatChannel.None;
		}
		goto IL_0031;
		IL_0031:
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
		case VoiceChatChannel.Spectator:
			if (Scp1576Item.ValidatedReceivers.Contains(base.Owner))
			{
				return VoiceChatChannel.Scp1576;
			}
			break;
		case VoiceChatChannel.Proximity:
			if (!this.CheckProximity(speaker))
			{
				break;
			}
			goto case VoiceChatChannel.Radio;
		case VoiceChatChannel.Radio:
		case VoiceChatChannel.Mimicry:
			return channel;
		case VoiceChatChannel.Scp1576:
			return VoiceChatChannel.Proximity;
		}
		return VoiceChatChannel.None;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this.RadioPlayback.Setup(base.Owner, this.ProximityPlaybacks);
		SingleBufferPlayback[] proximityPlaybacks = this.ProximityPlaybacks;
		for (int i = 0; i < proximityPlaybacks.Length; i++)
		{
			proximityPlaybacks[i].Source.mute = base.Owner.isLocalPlayer;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._wasTransmitting = false;
	}
}
