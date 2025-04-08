using System;
using CameraShaking;
using InventorySystem.Items.SwayControllers;
using PlayerRoles.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio
{
	public class RadioViewmodel : AnimatedViewmodelBase
	{
		public override IItemSwayController SwayController
		{
			get
			{
				return this._goopSway;
			}
		}

		public override float ViewmodelCameraFOV
		{
			get
			{
				return 50f;
			}
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			this.OnEquipped();
			if (!wasEquipped)
			{
				return;
			}
			base.GetComponent<AudioSource>().Stop();
			this.AnimatorForceUpdate(base.SkipEquipTime, true);
		}

		internal override void OnEquipped()
		{
			CameraShakeController.AddEffect(new TrackerShake(this._cameraTrackerSource, Quaternion.Euler(this._cameraTrackerOffset), this._cameraTrackerIntensity));
			this.RefreshKeypadColor(this._panelRoot.activeSelf);
		}

		private void Start()
		{
			this._goopSway = new GoopSway(new GoopSway.GoopSwaySettings(this._swayPivot, 0.65f, 0.0035f, 0.04f, 7f, 6.5f, 0.03f, 1.6f, false), base.Hub);
		}

		private void Update()
		{
			if (this._panelMain.activeSelf && this._panelRoot.activeSelf)
			{
				float num;
				this._textVolume.text = "-" + (this._voicechatMixer.GetFloat("AudioSettings_VoiceChat", out num) ? Mathf.Abs(Mathf.RoundToInt(num)) : 0).ToString();
				this._textTime.text = DateTime.Now.ToString("HH:mm:ss");
				bool flag;
				bool flag2;
				this.GetTxRx(out flag, out flag2);
				this._txOn.SetActive(flag);
				this._txOff.SetActive(!flag);
				this._rxOn.SetActive(flag2);
				this._rxOff.SetActive(!flag2);
				this.AnimatorSetBool(RadioViewmodel.IsTransmittingHash, flag);
			}
			else if (this._panelNoBattery.activeSelf)
			{
				this._batteryFlashTimer += Time.deltaTime;
				if (this._batteryFlashTimer > 0.4f)
				{
					this._batteryFlashTimer = 0f;
					this._noBatteryIndicator.enabled = !this._noBatteryIndicator.enabled;
				}
			}
			this.UpdateNetwork();
		}

		private void UpdateNetwork()
		{
			RadioStatusMessage radioStatusMessage;
			if (!RadioMessages.SyncedRangeLevels.TryGetValue(base.Hub.netId, out radioStatusMessage))
			{
				return;
			}
			this.SetBattery(radioStatusMessage.Battery);
			if (radioStatusMessage.Battery == 0)
			{
				return;
			}
			if (radioStatusMessage.Range == RadioMessages.RadioRangeLevel.RadioDisabled)
			{
				this.SetState(false);
				return;
			}
			this.SetState(true);
			this.SetRange((int)radioStatusMessage.Range);
		}

		private void GetTxRx(out bool tx, out bool rx)
		{
			tx = false;
			rx = false;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IVoiceRole voiceRole = referenceHub.roleManager.CurrentRole as IVoiceRole;
				if (voiceRole != null && PersonalRadioPlayback.IsTransmitting(referenceHub))
				{
					if (referenceHub == base.Hub)
					{
						tx = true;
					}
					else if (!(voiceRole.VoiceModule as IRadioVoiceModule).RadioPlayback.Source.mute)
					{
						rx = true;
					}
				}
			}
		}

		private void SetBattery(byte percent)
		{
			this._panelMain.SetActive(percent > 0);
			this._panelNoBattery.SetActive(percent == 0);
			if (percent > 0)
			{
				this._textBatteryLevel.text = percent.ToString() + "%";
				float num = (float)this._batteryLevels.Length / 100f;
				for (int i = 0; i < this._batteryLevels.Length; i++)
				{
					this._batteryLevels[i].enabled = Mathf.Round((float)percent * num) >= (float)(i + 1);
				}
				return;
			}
			this.AnimatorSetBool(RadioViewmodel.IsTransmittingHash, false);
		}

		private void SetRange(int rangeId)
		{
			if (this._prevRange != rangeId)
			{
				if (base.gameObject.activeInHierarchy)
				{
					this._audioSource.PlayOneShot(this._clipCircleRange);
				}
				this._prevRange = rangeId;
			}
			RadioItem radioItem;
			if (!InventoryItemLoader.TryGetItem<RadioItem>(ItemType.Radio, out radioItem))
			{
				return;
			}
			RadioRangeMode[] ranges = radioItem.Ranges;
			rangeId = Mathf.Clamp(rangeId, 0, ranges.Length - 1);
			this._textModeShort.text = ranges[rangeId].ShortName;
			this._textModeFull.text = ranges[rangeId].FullName;
			this._rangeIndicator.texture = ranges[rangeId].SignalTexture;
		}

		private void SetState(bool state)
		{
			if (this._panelRoot.activeSelf == state)
			{
				return;
			}
			this.RefreshKeypadColor(state);
			this._panelRoot.SetActive(state);
			this._audioSource.PlayOneShot(state ? this._clipTurnOn : this._clipTurnOff);
			if (!state)
			{
				this.AnimatorSetBool(RadioViewmodel.IsTransmittingHash, false);
			}
		}

		private void RefreshKeypadColor(bool state)
		{
			Material sharedMaterial = this._radioRenderer.sharedMaterial;
			if (sharedMaterial != this._enabledMat && sharedMaterial != this._disabledMat)
			{
				return;
			}
			this._radioRenderer.sharedMaterial = (state ? this._enabledMat : this._disabledMat);
		}

		[Header("World-space User Interface")]
		[SerializeField]
		private GameObject _panelNoBattery;

		[Header("World-space User Interface")]
		[SerializeField]
		private GameObject _panelMain;

		[Header("World-space User Interface")]
		[SerializeField]
		private GameObject _panelRoot;

		[SerializeField]
		private TMP_Text _textModeShort;

		[SerializeField]
		private TMP_Text _textModeFull;

		[SerializeField]
		private TMP_Text _textBatteryLevel;

		[SerializeField]
		private TMP_Text _textVolume;

		[SerializeField]
		private TMP_Text _textTime;

		[SerializeField]
		private GameObject _txOn;

		[SerializeField]
		private GameObject _txOff;

		[SerializeField]
		private GameObject _rxOn;

		[SerializeField]
		private GameObject _rxOff;

		[SerializeField]
		private RawImage _rangeIndicator;

		[SerializeField]
		private RawImage _noBatteryIndicator;

		[SerializeField]
		private Image[] _batteryLevels;

		[Header("Audio")]
		[SerializeField]
		private AudioMixer _voicechatMixer;

		[SerializeField]
		private AudioSource _audioSource;

		[SerializeField]
		private AudioClip _clipTurnOn;

		[SerializeField]
		private AudioClip _clipTurnOff;

		[SerializeField]
		private AudioClip _clipCircleRange;

		[Header("Tracker")]
		[SerializeField]
		private Transform _cameraTrackerSource;

		[SerializeField]
		private Vector3 _cameraTrackerOffset;

		[SerializeField]
		private float _cameraTrackerIntensity;

		[Header("Other")]
		[SerializeField]
		private Transform _swayPivot;

		[SerializeField]
		private MeshRenderer _radioRenderer;

		[SerializeField]
		private Material _enabledMat;

		[SerializeField]
		private Material _disabledMat;

		private static readonly int IsTransmittingHash = Animator.StringToHash("IsTransmitting");

		private const string RadioChannelName = "AudioSettings_VoiceChat";

		private const string KeypadEmissionChannelName = "_EmissionColor";

		private const float BatteryFlashRate = 2.5f;

		private GoopSway _goopSway;

		private float _batteryFlashTimer;

		private int _prevRange = -1;
	}
}
