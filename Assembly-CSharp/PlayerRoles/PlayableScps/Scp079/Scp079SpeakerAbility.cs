using Mirror;
using PlayerRoles.PlayableScps.Scp079.Map;
using UnityEngine;
using VoiceChat;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079SpeakerAbility : Scp079KeyAbilityBase, IScp079AuxRegenModifier
{
	[SerializeField]
	private float _regenMultiplier;

	private bool _syncUsing;

	private string _abilityName;

	private Scp079VoiceModule _voiceModule;

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

	public bool CanTransmit => !base.LostSignalHandler.Lost;

	public override ActionName ActivationKey => ActionName.AltVoiceChat;

	public override bool IsReady => true;

	public override bool IsVisible
	{
		get
		{
			if (!Scp079ToggleMenuAbilityBase<Scp079MapToggler>.Visible)
			{
				return this.CanTransmit;
			}
			return false;
		}
	}

	public override string AbilityName => this._abilityName;

	public override string FailMessage => null;

	private bool IsUsingSpeaker
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this._syncUsing;
			}
			bool serverIsSending = this._voiceModule.ServerIsSending;
			bool flag = VoiceChatMutes.IsMuted(base.Owner);
			VoiceChatChannel currentChannel = this._voiceModule.CurrentChannel;
			if (serverIsSending && !flag)
			{
				return currentChannel == VoiceChatChannel.Proximity;
			}
			return false;
		}
	}

	protected override void Trigger()
	{
	}

	private void RefreshNearestSpeaker()
	{
		if (base.CurrentCamSync.TryGetCurrentCamera(out var cam) && Scp079Speaker.TryGetSpeaker(cam, out var best))
		{
			this._voiceModule.ProximityPlayback.transform.position = best.Position;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._abilityName = Translations.Get(Scp079HudTranslation.UseSpeaker);
		this.AuxReductionMessage = Translations.Get(Scp079HudTranslation.SpeakerAuxPause);
		base.CurrentCamSync.OnCameraChanged += RefreshNearestSpeaker;
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active && this._syncUsing != this.IsUsingSpeaker)
		{
			base.ServerSendRpc(toAll: true);
		}
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
}
