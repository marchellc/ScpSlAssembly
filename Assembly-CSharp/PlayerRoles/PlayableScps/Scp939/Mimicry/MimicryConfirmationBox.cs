using CursorManagement;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using VoiceChat;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryConfirmationBox : MonoBehaviour, ICursorOverride
{
	private const string PrefsKey = "MimicryRememberChoice";

	[SerializeField]
	private GameObject _moreInfoRoot;

	[SerializeField]
	private Image _progress;

	[SerializeField]
	private float _duration;

	[SerializeField]
	private Canvas _hideHudCanvas;

	[SerializeField]
	private Toggle _rememberToggle;

	public static bool Remember
	{
		get
		{
			return PlayerPrefsSl.Get("MimicryRememberChoice", defaultValue: false);
		}
		set
		{
			PlayerPrefsSl.Set("MimicryRememberChoice", value);
		}
	}

	public CursorOverrideMode CursorOverride => CursorOverrideMode.Free;

	public bool LockMovement => false;

	public void ButtonOk()
	{
		if (this._rememberToggle.isOn)
		{
			MimicryConfirmationBox.Remember = true;
		}
		Object.Destroy(base.gameObject);
	}

	public void ButtonDelete()
	{
		VcPrivacyFlags vcPrivacyFlags = VoiceChatPrivacySettings.PrivacyFlags & ~VcPrivacyFlags.AllowRecording;
		if (this._rememberToggle.isOn)
		{
			VoiceChatPrivacySettings.PrivacyFlags = vcPrivacyFlags;
			VoiceChatPrivacySettings.Singleton.UpdateToggles();
		}
		NetworkClient.Send(new VoiceChatPrivacySettings.VcPrivacyMessage
		{
			Flags = (byte)vcPrivacyFlags
		});
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		if (this._moreInfoRoot.activeSelf)
		{
			this._progress.fillAmount = 1f;
			return;
		}
		this._progress.fillAmount -= Time.deltaTime / this._duration;
		if (!(this._progress.fillAmount > 0f))
		{
			Object.Destroy(base.gameObject);
		}
	}
}
