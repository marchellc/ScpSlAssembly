using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.NonAllocLINQ;
using VoiceChat.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryRecordingIcon : MonoBehaviour
	{
		public PlaybackBuffer VoiceRecord { get; private set; }

		public bool IsFavorite
		{
			get
			{
				return this._isFavorite;
			}
			private set
			{
				if (value == this.IsFavorite)
				{
					return;
				}
				this._isFavorite = value;
				if (value)
				{
					MimicryRecordingIcon.FavoritedEntries.Add(this);
					StaticUnityMethods.OnUpdate += this.UpdateFavoriteHotkey;
					return;
				}
				MimicryRecordingIcon.FavoritedEntries.Remove(this);
				StaticUnityMethods.OnUpdate -= this.UpdateFavoriteHotkey;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return this._isEmpty;
			}
			set
			{
				this._isEmpty = value;
				this._contentRoot.SetActive(!this._isEmpty);
				if (!value)
				{
					return;
				}
				this.IsFavorite = false;
			}
		}

		public float Height
		{
			get
			{
				return this._cachedRt.anchoredPosition.y;
			}
			set
			{
				this._cachedRt.anchoredPosition = value * Vector2.up;
			}
		}

		public void Setup(MimicryRecorder recorder, int id)
		{
			MimicryRecorder.MimicryRecording mimicryRecording = recorder.SavedVoices[id];
			this.VoiceRecord = mimicryRecording.Buffer;
			this._assignedRecorder = recorder;
			this._transmitter = recorder.Transmitter;
			this._previewPlayback = recorder.PreviewPlayback;
			this._waveformVisualizer.Generate(this.VoiceRecord.Buffer, 0, this.VoiceRecord.Length);
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(mimicryRecording.Owner.Role, out playerRoleBase))
			{
				return;
			}
			this._nickname.text = mimicryRecording.Owner.Nickname;
			this._rolename.text = playerRoleBase.RoleName;
			this._rolename.color = playerRoleBase.RoleColor;
		}

		private void Update()
		{
			if (this.IsEmpty)
			{
				return;
			}
			this._removeProgress.fillAmount = this._removeButton.HeldPercent;
			this._stopButton.interactable = this._waveformVisualizer.IsPlaying;
			if (!this._previewing || !this._previewPlayback.IsEmpty)
			{
				return;
			}
			this.StopPlayback();
		}

		private void Awake()
		{
			this._cachedRt = base.GetComponent<RectTransform>();
		}

		private void OnDestroy()
		{
			this.IsFavorite = false;
		}

		private void UpdateFavoriteHotkey()
		{
			if (!Input.GetKeyDown(this._assignedHotkey))
			{
				return;
			}
			this.SendRecording();
		}

		public void StartPreview()
		{
			if (this._previewing)
			{
				this.StopPreview();
			}
			int num;
			int num2;
			this._waveformVisualizer.StartPlayback(this.VoiceRecord.Length, out num, out num2);
			this._previewing = true;
			this._transmitter.StopTransmission();
			this._previewPlayback.StartPreview(this.VoiceRecord, num, num2);
		}

		public void RemoveRecording()
		{
			if (this._waveformVisualizer.IsPlaying)
			{
				this.StopPlayback();
			}
			this._assignedRecorder.RemoveVoice(this.VoiceRecord);
		}

		public void SendRecording()
		{
			this.StopPreview();
			int num;
			int num2;
			this._waveformVisualizer.StartPlayback(this.VoiceRecord.Length, out num, out num2);
			this._transmitter.SendVoice(this.VoiceRecord, num, num2);
		}

		public void StopPlayback()
		{
			this.StopPreview();
			this._transmitter.StopTransmission();
			this._waveformVisualizer.StopPlayback();
		}

		public void ToggleFavorite()
		{
			this.IsFavorite = !this.IsFavorite;
			this._favoriteFill.enabled = this.IsFavorite;
			this._favoriteLabel.enabled = this.IsFavorite;
			if (!this.IsFavorite)
			{
				return;
			}
			this._assignedHotkey = KeyCode.Alpha1;
			while (MimicryRecordingIcon.FavoritedEntries.Any((MimicryRecordingIcon x) => x.IsFavorite && x != this && x._assignedHotkey == this._assignedHotkey))
			{
				this._assignedHotkey++;
			}
			string text = string.Format("[ {0} ]", this._assignedHotkey - KeyCode.Alpha1 + 1);
			this._favoriteLabel.text = string.Format(Translations.Get<Scp939HudTranslation>(Scp939HudTranslation.HintAssignedHotkey, "Hotkey  {0}"), text);
		}

		private void StopPreview()
		{
			if (!this._previewing)
			{
				return;
			}
			this._previewing = false;
			this._previewPlayback.StopPreview();
		}

		private static readonly HashSet<MimicryRecordingIcon> FavoritedEntries = new HashSet<MimicryRecordingIcon>();

		[SerializeField]
		private GameObject _contentRoot;

		[SerializeField]
		private TextMeshProUGUI _nickname;

		[SerializeField]
		private TextMeshProUGUI _rolename;

		[SerializeField]
		private Image _removeProgress;

		[SerializeField]
		private HoldableButton _removeButton;

		[SerializeField]
		private HoldableButton _previewButton;

		[SerializeField]
		private HoldableButton _useButton;

		[SerializeField]
		private HoldableButton _stopButton;

		[SerializeField]
		private MimicryWaveform _waveformVisualizer;

		[SerializeField]
		private Image _favoriteFill;

		[SerializeField]
		private TMP_Text _favoriteLabel;

		private const KeyCode FirstKeybind = KeyCode.Alpha1;

		private MimicryPreviewPlayback _previewPlayback;

		private MimicryTransmitter _transmitter;

		private MimicryRecorder _assignedRecorder;

		private RectTransform _cachedRt;

		private bool _previewing;

		private bool _isEmpty;

		private bool _isFavorite;

		private KeyCode _assignedHotkey;
	}
}
