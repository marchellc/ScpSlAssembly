using System;
using System.Collections.Generic;
using Footprinting;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Rewards;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;
using VoiceChat;
using VoiceChat.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryRecorder : StandardSubroutine<Scp939Role>
{
	public readonly struct MimicryRecording
	{
		public readonly Footprint Owner;

		public readonly PlaybackBuffer Buffer;

		public MimicryRecording(ReferenceHub owner, PlaybackBuffer buffer)
		{
			this.Owner = new Footprint(owner);
			this.Buffer = buffer;
			this.Buffer.Reorganize();
		}
	}

	private readonly Dictionary<HumanRole, PlaybackBuffer> _received = new Dictionary<HumanRole, PlaybackBuffer>();

	private readonly HashSet<ReferenceHub> _serverSentVoices = new HashSet<ReferenceHub>();

	private readonly HashSet<ReferenceHub> _serverSentConfirmations = new HashSet<ReferenceHub>();

	private bool _wasLocal;

	private bool _syncMute;

	private ReferenceHub _syncPlayer;

	[SerializeField]
	private int _maxDurationSamples;

	[SerializeField]
	private int _minDurationSamples;

	[SerializeField]
	private GameObject _confirmationBox;

	private const double MarkUptime = 5.0;

	private readonly List<Footprint> _lastAttackedPlayers = new List<Footprint>();

	public readonly List<MimicryRecording> SavedVoices = new List<MimicryRecording>();

	[field: SerializeField]
	public int MaxRecordings { get; private set; }

	[field: SerializeField]
	public MimicryPreviewPlayback PreviewPlayback { get; private set; }

	[field: SerializeField]
	public MimicryTransmitter Transmitter { get; private set; }

	public event Action OnSavedVoicesModified;

	public event Action<Scp939HudTranslation> OnSavedVoicesItemAdded;

	private void OnRoleChanged(ReferenceHub ply, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (prevRole is HumanRole role)
		{
			this.UnregisterRole(role);
		}
		if (newRole is HumanRole role2)
		{
			this.RegisterRole(role2);
		}
	}

	private void ServerRemoveClient(ReferenceHub ply)
	{
		this._syncMute = true;
		this._syncPlayer = ply;
		base.ServerSendRpc(toAll: false);
		this.RemoveRecordingsOfPlayer(ply);
	}

	private void RemoveRecordingsOfPlayer(ReferenceHub ply)
	{
		this._serverSentVoices.Remove(ply);
		for (int i = 0; i < this.SavedVoices.Count; i++)
		{
			if (!(this.SavedVoices[i].Owner.Hub != ply))
			{
				this.RemoveIndex(i--);
			}
		}
	}

	private void OnAnyPlayerWasDamaged(ReferenceHub ply, DamageHandlerBase dh)
	{
		if (!VoiceChatMutes.IsMuted(ply) && this.IsPrivacyAccepted(ply) && dh is Scp939DamageHandler scp939DamageHandler && !(scp939DamageHandler.Attacker.Hub != base.Owner))
		{
			this._lastAttackedPlayers.Add(new Footprint(ply));
		}
	}

	private void ClearExpiredTargets()
	{
		for (int num = this._lastAttackedPlayers.Count - 1; num >= 0; num--)
		{
			Footprint footprint = this._lastAttackedPlayers[num];
			if (footprint.Hub == null || (double)footprint.Stopwatch.Elapsed.Seconds > 5.0)
			{
				this._lastAttackedPlayers.RemoveAt(num);
			}
		}
	}

	private bool WasAttackedRecently(ReferenceHub ply)
	{
		this.ClearExpiredTargets();
		foreach (Footprint lastAttackedPlayer in this._lastAttackedPlayers)
		{
			if (lastAttackedPlayer.Hub == ply)
			{
				return true;
			}
		}
		return false;
	}

	private void OnAnyPlayerKilled(ReferenceHub ply, DamageHandlerBase dh)
	{
		if (!VoiceChatMutes.IsMuted(ply) && this.IsPrivacyAccepted(ply) && this.WasAttackedRecently(ply) && ply.roleManager.CurrentRole is HumanRole ply2 && this.WasKilledByTeammate(ply2, dh))
		{
			this._syncPlayer = ply;
			this._syncMute = false;
			if (base.Owner.isLocalPlayer)
			{
				this.SaveRecording(ply);
			}
			else
			{
				base.ServerSendRpc(toAll: false);
			}
			this._serverSentVoices.Add(ply);
			this._serverSentConfirmations.Remove(ply);
		}
	}

	private bool WasKilledByTeammate(HumanRole ply, DamageHandlerBase dh)
	{
		if (dh is AttackerDamageHandler attackerDamageHandler)
		{
			return attackerDamageHandler.Attacker.Role.GetTeam() == Team.SCPs;
		}
		if (!(dh is UniversalDamageHandler universalDamageHandler))
		{
			return false;
		}
		bool flag = Scp079RewardManager.CheckForRoomInteractions(ply.FpcModule.Position);
		if (universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id && flag)
		{
			return true;
		}
		if (universalDamageHandler.TranslationId == DeathTranslations.PocketDecay.Id)
		{
			return true;
		}
		return false;
	}

	private void OnPlayerMuteChanges(ReferenceHub ply, VcMuteFlags _)
	{
		if (VoiceChatMutes.IsMuted(ply) && this._serverSentVoices.Remove(ply))
		{
			this.ServerRemoveClient(ply);
		}
	}

	private void OnPlayerPrivacyChanges(ReferenceHub ply)
	{
		if (!this.IsPrivacyAccepted(ply) && this._serverSentVoices.Remove(ply))
		{
			this.ServerRemoveClient(ply);
		}
	}

	private bool IsPrivacyAccepted(ReferenceHub hub)
	{
		return VoiceChatPrivacySettings.CheckUserFlags(hub, VcPrivacyFlags.SettingsSelected | VcPrivacyFlags.AllowMicCapture | VcPrivacyFlags.AllowRecording);
	}

	private void UnregisterRole(HumanRole role)
	{
		if (this._received.TryGetValue(role, out var value))
		{
			role.VoiceModule.OnSamplesReceived -= value.Write;
			this._received.Remove(role);
		}
	}

	private void RegisterRole(HumanRole role)
	{
		PlaybackBuffer playbackBuffer = new PlaybackBuffer(this._maxDurationSamples, endlessTapeMode: true);
		this._received[role] = playbackBuffer;
		role.VoiceModule.OnSamplesReceived += playbackBuffer.Write;
	}

	private void SaveRecording(ReferenceHub ply)
	{
		if (ply.roleManager.CurrentRole is HumanRole key && this._received.TryGetValue(key, out var value) && value.Length >= this._minDurationSamples)
		{
			this.SavedVoices.Add(new MimicryRecording(ply, value));
			this.OnSavedVoicesItemAdded?.Invoke(Scp939HudTranslation.YouGotAVoicelinePopup);
			this.OnSavedVoicesModified?.Invoke();
			base.ClientSendCmd();
		}
	}

	public void RemoveVoice(PlaybackBuffer voiceRecord)
	{
		for (int i = 0; i < this.SavedVoices.Count; i++)
		{
			if (this.SavedVoices[i].Buffer == voiceRecord)
			{
				this.RemoveIndex(i--);
			}
		}
	}

	public void RemoveIndex(int id)
	{
		this.SavedVoices.RemoveAt(id);
		this.OnSavedVoicesModified?.Invoke();
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this._syncMute);
		writer.WriteReferenceHub(this._syncPlayer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._syncMute = reader.ReadBool();
		this._syncPlayer = reader.ReadReferenceHub();
		if (this._syncPlayer == null)
		{
			return;
		}
		if (this._syncPlayer.isLocalPlayer)
		{
			if (!MimicryConfirmationBox.Remember)
			{
				UnityEngine.Object.Instantiate(this._confirmationBox);
			}
		}
		else if (this._syncMute)
		{
			this.RemoveRecordingsOfPlayer(this._syncPlayer);
		}
		else
		{
			this.SaveRecording(this._syncPlayer);
		}
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteReferenceHub(this._syncPlayer);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		ReferenceHub rh = reader.ReadReferenceHub();
		if (this._serverSentVoices.Contains(rh) && this._serverSentConfirmations.Add(rh))
		{
			base.ServerSendRpc((ReferenceHub x) => x == rh);
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		PlayerStats.OnAnyPlayerDamaged += OnAnyPlayerWasDamaged;
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerKilled;
		VoiceChatMutes.OnFlagsSet += OnPlayerMuteChanges;
		VoiceChatPrivacySettings.OnUserFlagsChanged += OnPlayerPrivacyChanges;
		if (!base.Owner.isLocalPlayer)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is HumanRole role)
			{
				this.RegisterRole(role);
			}
		}
		this._wasLocal = true;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		ReferenceHub.OnPlayerRemoved += RemoveRecordingsOfPlayer;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		PlayerStats.OnAnyPlayerDamaged -= OnAnyPlayerWasDamaged;
		PlayerStats.OnAnyPlayerDied -= OnAnyPlayerKilled;
		VoiceChatMutes.OnFlagsSet -= OnPlayerMuteChanges;
		VoiceChatPrivacySettings.OnUserFlagsChanged -= OnPlayerPrivacyChanges;
		this._serverSentVoices.Clear();
		this._serverSentConfirmations.Clear();
		if (this._wasLocal)
		{
			this._wasLocal = false;
			this.SavedVoices.Clear();
			this._received.Clear();
			this.PreviewPlayback.StopPreview();
			PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
			ReferenceHub.OnPlayerRemoved -= RemoveRecordingsOfPlayer;
		}
	}
}
