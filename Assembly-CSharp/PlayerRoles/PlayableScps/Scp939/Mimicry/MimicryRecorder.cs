using System;
using System.Collections.Generic;
using Footprinting;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Rewards;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;
using VoiceChat;
using VoiceChat.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryRecorder : StandardSubroutine<Scp939Role>
	{
		public int MaxRecordings { get; private set; }

		public MimicryPreviewPlayback PreviewPlayback { get; private set; }

		public MimicryTransmitter Transmitter { get; private set; }

		public event Action OnSavedVoicesModified;

		public event Action<Scp939HudTranslation> OnSavedVoicesItemAdded;

		private void OnRoleChanged(ReferenceHub ply, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			HumanRole humanRole = prevRole as HumanRole;
			if (humanRole != null)
			{
				this.UnregisterRole(humanRole);
			}
			HumanRole humanRole2 = newRole as HumanRole;
			if (humanRole2 != null)
			{
				this.RegisterRole(humanRole2);
			}
		}

		private void ServerRemoveClient(ReferenceHub ply)
		{
			this._syncMute = true;
			this._syncPlayer = ply;
			base.ServerSendRpc(false);
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
			if (VoiceChatMutes.IsMuted(ply, false) || !this.IsPrivacyAccepted(ply))
			{
				return;
			}
			Scp939DamageHandler scp939DamageHandler = dh as Scp939DamageHandler;
			if (scp939DamageHandler == null)
			{
				return;
			}
			if (scp939DamageHandler.Attacker.Hub != base.Owner)
			{
				return;
			}
			this._lastAttackedPlayers.Add(new Footprint(ply));
		}

		private void ClearExpiredTargets()
		{
			for (int i = this._lastAttackedPlayers.Count - 1; i >= 0; i--)
			{
				Footprint footprint = this._lastAttackedPlayers[i];
				if (footprint.Hub == null || (double)footprint.Stopwatch.Elapsed.Seconds > 5.0)
				{
					this._lastAttackedPlayers.RemoveAt(i);
				}
			}
		}

		private bool WasAttackedRecently(ReferenceHub ply)
		{
			this.ClearExpiredTargets();
			using (List<Footprint>.Enumerator enumerator = this._lastAttackedPlayers.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Hub == ply)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void OnAnyPlayerKilled(ReferenceHub ply, DamageHandlerBase dh)
		{
			if (VoiceChatMutes.IsMuted(ply, false) || !this.IsPrivacyAccepted(ply))
			{
				return;
			}
			if (!this.WasAttackedRecently(ply))
			{
				return;
			}
			HumanRole humanRole = ply.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			if (!this.WasKilledByTeammate(humanRole, dh))
			{
				return;
			}
			this._syncPlayer = ply;
			this._syncMute = false;
			if (base.Owner.isLocalPlayer)
			{
				this.SaveRecording(ply);
			}
			else
			{
				base.ServerSendRpc(false);
			}
			this._serverSentVoices.Add(ply);
			this._serverSentConfirmations.Remove(ply);
		}

		private bool WasKilledByTeammate(HumanRole ply, DamageHandlerBase dh)
		{
			AttackerDamageHandler attackerDamageHandler = dh as AttackerDamageHandler;
			if (attackerDamageHandler != null)
			{
				return attackerDamageHandler.Attacker.Role.GetTeam() == Team.SCPs;
			}
			UniversalDamageHandler universalDamageHandler = dh as UniversalDamageHandler;
			if (universalDamageHandler == null)
			{
				return false;
			}
			bool flag = Scp079RewardManager.CheckForRoomInteractions(RoomUtils.RoomAtPositionRaycasts(ply.FpcModule.Position, true));
			return (universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id && flag) || universalDamageHandler.TranslationId == DeathTranslations.PocketDecay.Id;
		}

		private void OnPlayerMuteChanges(ReferenceHub ply, VcMuteFlags _)
		{
			if (!VoiceChatMutes.IsMuted(ply, false) || !this._serverSentVoices.Remove(ply))
			{
				return;
			}
			this.ServerRemoveClient(ply);
		}

		private void OnPlayerPrivacyChanges(ReferenceHub ply)
		{
			if (this.IsPrivacyAccepted(ply) || !this._serverSentVoices.Remove(ply))
			{
				return;
			}
			this.ServerRemoveClient(ply);
		}

		private bool IsPrivacyAccepted(ReferenceHub hub)
		{
			return VoiceChatPrivacySettings.CheckUserFlags(hub, VcPrivacyFlags.SettingsSelected | VcPrivacyFlags.AllowMicCapture | VcPrivacyFlags.AllowRecording);
		}

		private void UnregisterRole(HumanRole role)
		{
			PlaybackBuffer playbackBuffer;
			if (!this._received.TryGetValue(role, out playbackBuffer))
			{
				return;
			}
			role.VoiceModule.OnSamplesReceived -= playbackBuffer.Write;
			this._received.Remove(role);
		}

		private void RegisterRole(HumanRole role)
		{
			PlaybackBuffer playbackBuffer = new PlaybackBuffer(this._maxDurationSamples, true);
			this._received[role] = playbackBuffer;
			role.VoiceModule.OnSamplesReceived += playbackBuffer.Write;
		}

		private void SaveRecording(ReferenceHub ply)
		{
			HumanRole humanRole = ply.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			PlaybackBuffer playbackBuffer;
			if (!this._received.TryGetValue(humanRole, out playbackBuffer))
			{
				return;
			}
			if (playbackBuffer.Length < this._minDurationSamples)
			{
				return;
			}
			this.SavedVoices.Add(new MimicryRecorder.MimicryRecording(ply, playbackBuffer));
			Action<Scp939HudTranslation> onSavedVoicesItemAdded = this.OnSavedVoicesItemAdded;
			if (onSavedVoicesItemAdded != null)
			{
				onSavedVoicesItemAdded(Scp939HudTranslation.YouGotAVoicelinePopup);
			}
			Action onSavedVoicesModified = this.OnSavedVoicesModified;
			if (onSavedVoicesModified != null)
			{
				onSavedVoicesModified();
			}
			base.ClientSendCmd();
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
			Action onSavedVoicesModified = this.OnSavedVoicesModified;
			if (onSavedVoicesModified == null)
			{
				return;
			}
			onSavedVoicesModified();
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
				if (MimicryConfirmationBox.Remember)
				{
					return;
				}
				global::UnityEngine.Object.Instantiate<GameObject>(this._confirmationBox);
				return;
			}
			else
			{
				if (this._syncMute)
				{
					this.RemoveRecordingsOfPlayer(this._syncPlayer);
					return;
				}
				this.SaveRecording(this._syncPlayer);
				return;
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
			if (!this._serverSentVoices.Contains(rh))
			{
				return;
			}
			if (!this._serverSentConfirmations.Add(rh))
			{
				return;
			}
			base.ServerSendRpc((ReferenceHub x) => x == rh);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			PlayerStats.OnAnyPlayerDamaged += this.OnAnyPlayerWasDamaged;
			PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerKilled;
			VoiceChatMutes.OnFlagsSet += this.OnPlayerMuteChanges;
			VoiceChatPrivacySettings.OnUserFlagsChanged += this.OnPlayerPrivacyChanges;
			if (!base.Owner.isLocalPlayer)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
				if (humanRole != null)
				{
					this.RegisterRole(humanRole);
				}
			}
			this._wasLocal = true;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.RemoveRecordingsOfPlayer));
		}

		public override void ResetObject()
		{
			base.ResetObject();
			PlayerStats.OnAnyPlayerDamaged -= this.OnAnyPlayerWasDamaged;
			PlayerStats.OnAnyPlayerDied -= this.OnAnyPlayerKilled;
			VoiceChatMutes.OnFlagsSet -= this.OnPlayerMuteChanges;
			VoiceChatPrivacySettings.OnUserFlagsChanged -= this.OnPlayerPrivacyChanges;
			this._serverSentVoices.Clear();
			this._serverSentConfirmations.Clear();
			if (!this._wasLocal)
			{
				return;
			}
			this._wasLocal = false;
			this.SavedVoices.Clear();
			this._received.Clear();
			this.PreviewPlayback.StopPreview();
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.RemoveRecordingsOfPlayer));
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

		public readonly List<MimicryRecorder.MimicryRecording> SavedVoices = new List<MimicryRecorder.MimicryRecording>();

		public readonly struct MimicryRecording
		{
			public MimicryRecording(ReferenceHub owner, PlaybackBuffer buffer)
			{
				this.Owner = new Footprint(owner);
				this.Buffer = buffer;
				this.Buffer.Reorganize();
			}

			public readonly Footprint Owner;

			public readonly PlaybackBuffer Buffer;
		}
	}
}
