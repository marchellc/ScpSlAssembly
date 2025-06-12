using UnityEngine;

namespace VoiceChat;

public class VoiceChatInGameSettings : MonoBehaviour
{
	[SerializeField]
	private GameObject _acceptedRoot;

	[SerializeField]
	private GameObject _deniedRoot;

	[SerializeField]
	private bool _updateEveryFrame;

	private void Awake()
	{
		this.UpdateSettings();
		VoiceChatPrivacySettings.OnUserFlagsChanged += OnUserFlagsChanged;
	}

	private void OnDestroy()
	{
		VoiceChatPrivacySettings.OnUserFlagsChanged -= OnUserFlagsChanged;
	}

	private void Update()
	{
		if (this._updateEveryFrame)
		{
			this.UpdateSettings();
		}
	}

	private void OnUserFlagsChanged(ReferenceHub hub)
	{
		if (hub.isLocalPlayer)
		{
			this.UpdateSettings();
		}
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
}
