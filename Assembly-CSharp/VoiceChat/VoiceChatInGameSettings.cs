using System;
using UnityEngine;

namespace VoiceChat
{
	public class VoiceChatInGameSettings : MonoBehaviour
	{
		private void Awake()
		{
			this.UpdateSettings();
			VoiceChatPrivacySettings.OnUserFlagsChanged += this.OnUserFlagsChanged;
		}

		private void OnDestroy()
		{
			VoiceChatPrivacySettings.OnUserFlagsChanged -= this.OnUserFlagsChanged;
		}

		private void Update()
		{
			if (!this._updateEveryFrame)
			{
				return;
			}
			this.UpdateSettings();
		}

		private void OnUserFlagsChanged(ReferenceHub hub)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			this.UpdateSettings();
		}

		private void UpdateSettings()
		{
			bool flag = (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowMicCapture) == VcPrivacyFlags.AllowMicCapture;
			this._acceptedRoot.SetActive(flag);
			this._deniedRoot.SetActive(!flag);
		}

		public void ShowPrivacySettings()
		{
		}

		[SerializeField]
		private GameObject _acceptedRoot;

		[SerializeField]
		private GameObject _deniedRoot;

		[SerializeField]
		private bool _updateEveryFrame;
	}
}
